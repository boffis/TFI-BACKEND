using GymManagement.Application.Exceptions;
using GymManagement.Application.Requests;
using GymManagement.Domain.Entities;
using GymManagement.Infrastructure.Persistence;
using GymManagement.Infrastructure.Settings;
using MercadoPago.Client.Common;
using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;
using MercadoPago.Client.Preapproval;
using MercadoPago.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GymManagement.Infrastructure.Payments
{
    public class MercadoPagoService
    {
        private readonly MercadoPagoSettings _settings;
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public MercadoPagoService(
            IOptions<MercadoPagoSettings> settings,
            ApplicationDbContext context,
            Microsoft.Extensions.Configuration.IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _settings = settings.Value;
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            MercadoPagoConfig.AccessToken = _settings.AccessToken?.Trim();
        }

        // ─── Signature Validation ───────────────────────────────────────────────────

        /// <summary>
        /// Validates the x-signature header sent by Mercado Pago using HMAC-SHA256 and WebhookSecret.
        /// Returns true if valid, or if WebhookSecret is empty (local dev bypass).
        /// </summary>
        public bool ValidateWebhookSignature(string? xSignature, string? requestId, string? dataId)
        {
            if (string.IsNullOrWhiteSpace(_settings.WebhookSecret))
            {
                // Secret not configured (e.g. local development), bypass check
                return true;
            }

            if (string.IsNullOrWhiteSpace(xSignature) || string.IsNullOrWhiteSpace(dataId))
            {
                return false;
            }

            try
            {
                string? ts = null;
                string? v1 = null;

                var parts = xSignature.Split(',');
                foreach (var part in parts)
                {
                    var keyValue = part.Trim().Split('=', 2);
                    if (keyValue.Length == 2)
                    {
                        if (keyValue[0] == "ts") ts = keyValue[1];
                        if (keyValue[0] == "v1") v1 = keyValue[1];
                    }
                }

                if (string.IsNullOrEmpty(ts) || string.IsNullOrEmpty(v1)) return false;

                var manifest = $"id:{dataId};request-id:{requestId ?? ""};ts:{ts};";

                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.WebhookSecret));
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest));
                var hashHex = Convert.ToHexString(hashBytes).ToLowerInvariant();

                return hashHex.Equals(v1, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        // ─── Legacy redirect-based flow (kept for reference) ────────────────────────

        public async Task<string> CreatePreference(Guid membershipId)
        {
            var membership = await _context.Memberships
                .Include(m => m.MembershipPlan)
                .FirstOrDefaultAsync(m => m.MembershipId == membershipId);

            if (membership == null)
                throw new NotFoundException($"Membership {membershipId} no encontrada.");

            if (membership.MembershipPlan == null)
                throw new Exception("Membership does not have an associated plan.");

            var price = membership.MembershipPlan.Price;

            var clientAppUrl = (_configuration["EmailSettings:ClientAppUrl"] ?? string.Empty).TrimEnd('/');

            var request = new PreferenceRequest
            {
                Items = new List<PreferenceItemRequest>
                {
                    new PreferenceItemRequest
                    {
                        Title = $"Membresía {membership.MembershipPlan.Type}",
                        Quantity = 1,
                        CurrencyId = "ARS",
                        UnitPrice = price
                    }
                },
                BackUrls = new PreferenceBackUrlsRequest
                {
                    Success = clientAppUrl,
                    Failure = clientAppUrl,
                    Pending = clientAppUrl
                },
                AutoReturn = "approved"
            };

            var client = new PreferenceClient();
            var preference = await client.CreateAsync(request);

            var payment = new Payment
            {
                PaymentId = Guid.NewGuid(),
                UserId = membership.UserId,
                User = null!,
                MembershipId = membershipId,
                Membership = membership,
                Price = price,
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = "MercadoPago",
                PaymentState = "pending"
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return preference.InitPoint;
        }

        public async Task ProcessWebhook(Guid paymentId, string status)
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment == null) return;

            payment.PaymentState = status;
            await _context.SaveChangesAsync();

            if (status == "approved")
            {
                var membership = await _context.Memberships
                    .Include(m => m.MembershipPlan)
                    .FirstOrDefaultAsync(m => m.MembershipId == payment.MembershipId);

                if (membership != null && membership.MembershipPlan != null)
                {
                    var durationDays = membership.MembershipPlan.DurationInDays;
                    membership.ExpirationDate = DateTime.UtcNow.AddDays(durationDays);
                    await _context.SaveChangesAsync();
                }
            }
        }

        // ─── Card Payment Brick / Subscriptions flow ─────────────────────────────────

        /// <summary>
        /// Processes the initial subscription payment using the card token from the
        /// Card Payment Brick, activates the user's membership, and creates a recurring
        /// Preapproval in Mercado Pago for future automatic charges.
        /// </summary>
        public async Task<object> CreateSubscriptionAsync(SubscriptionRequest request, Guid userId)
        {
            if (string.IsNullOrWhiteSpace(request.PaymentMethodId))
            {
                throw new ValidationException("El campo 'payment_method_id' es requerido.");
            }

            if (string.IsNullOrWhiteSpace(request.Token))
            {
                throw new ValidationException("El campo 'token' de tarjeta es requerido.");
            }

            var plan = await _context.MembershipPlans
                .FirstOrDefaultAsync(p => p.MembershipPlanId == request.MembershipPlanId)
                ?? throw new NotFoundException($"Plan {request.MembershipPlanId} no encontrado.");

            // ── Step 1: Create Preapproval (subscription) in Mercado Pago via raw HTTP ──
            // The SDK v3.3.0 is missing card_token_id on PreapprovalCreateRequest,
            // so we call the API directly with HttpClient to include all required fields.
            var (frequencyType, frequencyValue) = plan.DurationInDays switch
            {
                7 => ("days", 7),
                30 => ("months", 1),
                365 => ("months", 12),
                _ => ("days", plan.DurationInDays)
            };

            var rawClientAppUrl = _configuration["EmailSettings:ClientAppUrl"];
            var clientAppUrl = string.IsNullOrWhiteSpace(rawClientAppUrl) ? "https://google.com" : rawClientAppUrl.TrimEnd('/');

            var preapprovalBody = new
            {
                back_url = clientAppUrl,
                reason = $"Membresía {plan.Type} - Gym Management",
                external_reference = userId.ToString(),
                payer_email = request.Payer.Email,
                card_token_id = request.Token,
                status = "authorized",
                auto_recurring = new
                {
                    frequency = frequencyValue,
                    frequency_type = frequencyType,
                    start_date = DateTime.UtcNow.AddDays(plan.DurationInDays).ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"),
                    end_date = DateTime.UtcNow.AddYears(3).ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"),
                    transaction_amount = plan.Price,
                    currency_id = "ARS"
                }
            };

            string? preapprovalId = null;
            try
            {
                var http = _httpClientFactory.CreateClient();
                var json = JsonSerializer.Serialize(preapprovalBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.AccessToken?.Trim()}");

                var response = await http.PostAsync("https://api.mercadopago.com/preapproval", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new ValidationException($"Error al suscribir en Mercado Pago: {responseBody}");

                using var doc = JsonDocument.Parse(responseBody);
                preapprovalId = doc.RootElement.GetProperty("id").GetString();
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ValidationException($"Error al crear la suscripción en Mercado Pago: {ex.Message}");
            }

            // ── Step 2: Activate the membership locally ────────────────────────────
            // MP automatically charges the card within 1 hour of subscription creation.
            // We record the membership and a pending payment locally.
            var membership = await _context.Memberships
                .FirstOrDefaultAsync(m => m.UserId == userId && !m.IsCancelled);

            if (membership == null)
            {
                membership = new Membership
                {
                    MembershipId = Guid.NewGuid(),
                    UserId = userId,
                    User = null!,
                    MembershipPlanId = plan.MembershipPlanId,
                    MembershipPlan = null!
                };
                _context.Memberships.Add(membership);
            }
            else
            {
                membership.MembershipPlanId = plan.MembershipPlanId;
            }

            membership.IsCancelled = false;
            membership.MpPreapprovalId = preapprovalId;
            // ExpirationDate will be updated by webhook when MP confirms the payment.
            // Set a provisional expiration based on plan duration.
            membership.ExpirationDate = DateTime.UtcNow.AddDays(plan.DurationInDays);

            var localPayment = new Payment
            {
                PaymentId = Guid.NewGuid(),
                UserId = userId,
                User = null!,
                MembershipId = membership.MembershipId,
                Membership = null!,
                Price = plan.Price,
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = request.PaymentMethodId,
                PaymentState = "pending",
                MpPaymentId = preapprovalId
            };

            _context.Payments.Add(localPayment);
            await _context.SaveChangesAsync();

            return new
            {
                preapprovalId = preapprovalId,
                paymentStatus = "pending",
                membershipId = membership.MembershipId,
                expirationDate = membership.ExpirationDate
            };
        }

        /// <summary>
        /// Cancels a recurring subscription both in Mercado Pago and locally.
        /// Only the owner of the membership may cancel it (userId is validated here).
        /// Uses the named "MercadoPago" HttpClient which has a Polly retry policy
        /// (up to 5 retries with exponential backoff on transient HTTP errors).
        /// IsCancelled is only set to true once Mercado Pago confirms the cancellation.
        /// </summary>
        public async Task CancelSubscriptionAsync(Guid membershipId, Guid userId)
        {
            var membership = await _context.Memberships
                .FirstOrDefaultAsync(m => m.MembershipId == membershipId)
                ?? throw new NotFoundException($"Membresía {membershipId} no encontrada.");

            if (membership.UserId != userId)
                throw new UnauthorizedException("No tienes permiso para cancelar esta membresía.");

            if (membership.IsCancelled)
                throw new Exception("Esta membresía ya está cancelada.");

            if (!string.IsNullOrEmpty(membership.MpPreapprovalId))
            {
                // Use the named HttpClient which carries the Polly retry policy.
                // We call the MP REST API directly (PATCH /preapproval/{id}) because the SDK
                // does not support the retry pipeline. On any non-2xx after 5 attempts, this throws.
                var http = _httpClientFactory.CreateClient("MercadoPago");
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.AccessToken?.Trim()}");

                var body = JsonSerializer.Serialize(new { status = "cancelled" });
                var content = new StringContent(body, Encoding.UTF8, "application/json");

                var response = await http.PatchAsync(
                    $"https://api.mercadopago.com/preapproval/{membership.MpPreapprovalId}",
                    content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Mercado Pago no pudo cancelar la suscripción: {error}");
                }
            }

            // Only mark as cancelled locally after MP confirmed the cancellation above.
            membership.IsCancelled = true;
            await _context.SaveChangesAsync();
        }

        // ─── Webhook Notification Processing ─────────────────────────────────────────

        /// <summary>
        /// Handles asynchronous Webhook notifications sent by Mercado Pago.
        /// Follows official MP Webhook documentation:
        /// 1. Query resource details via SDK using resourceId.
        /// 2. If approved payment -> record payment and extend membership expiration.
        /// 3. If preapproval status update -> sync local cancellation state.
        /// </summary>
        public async Task ProcessWebhookNotificationAsync(string? resourceType, string resourceId)
        {
            if (string.IsNullOrWhiteSpace(resourceId)) return;

            resourceType = resourceType?.ToLowerInvariant();

            if (resourceType == "payment" || resourceType == "subscription_authorized_payment")
            {
                if (!long.TryParse(resourceId, out long mpPaymentId)) return;

                // Idempotency check: has this payment already been processed?
                var existingPayment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.MpPaymentId == resourceId);

                if (existingPayment != null)
                {
                    // Payment already recorded
                    return;
                }

                // Fetch real-time payment status directly from Mercado Pago SDK
                var paymentClient = new PaymentClient();
                var mpPayment = await paymentClient.GetAsync(mpPaymentId);

                if (mpPayment == null) return;

                if (mpPayment.Status == "approved")
                {
                    if (Guid.TryParse(mpPayment.ExternalReference, out Guid userId))
                    {
                        var membership = await _context.Memberships
                            .Include(m => m.MembershipPlan)
                            .FirstOrDefaultAsync(m => m.UserId == userId && !m.IsCancelled);

                        if (membership != null && membership.MembershipPlan != null)
                        {
                            // Extend membership expiration
                            membership.ExpirationDate = DateTime.UtcNow > membership.ExpirationDate
                                ? DateTime.UtcNow.AddDays(membership.MembershipPlan.DurationInDays)
                                : membership.ExpirationDate.AddDays(membership.MembershipPlan.DurationInDays);

                            // Try to find the existing pending payment for this membership
                            // (created locally when the subscription was initiated) and update it.
                            // This avoids duplicate records — we update in place instead of inserting a new one.
                            var pendingPayment = await _context.Payments
                                .FirstOrDefaultAsync(p =>
                                    p.MembershipId == membership.MembershipId &&
                                    p.PaymentState == "pending");

                            if (pendingPayment != null)
                            {
                                pendingPayment.PaymentState = mpPayment.Status;
                                pendingPayment.MpPaymentId = resourceId;
                                pendingPayment.PaymentDate = DateTime.UtcNow;
                                pendingPayment.PaymentMethod = mpPayment.PaymentMethodId ?? pendingPayment.PaymentMethod;
                                pendingPayment.Price = mpPayment.TransactionAmount ?? pendingPayment.Price;
                            }
                            else
                            {
                                // No pending record found (edge case): insert a new payment
                                var payment = new Payment
                                {
                                    PaymentId = Guid.NewGuid(),
                                    UserId = userId,
                                    User = null!,
                                    MembershipId = membership.MembershipId,
                                    Membership = null!,
                                    Price = mpPayment.TransactionAmount ?? membership.MembershipPlan.Price,
                                    PaymentDate = DateTime.UtcNow,
                                    PaymentMethod = mpPayment.PaymentMethodId ?? "MercadoPago",
                                    PaymentState = mpPayment.Status,
                                    MpPaymentId = resourceId
                                };
                                _context.Payments.Add(payment);
                            }

                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }
            else if (resourceType == "subscription_preapproval" || resourceType == "preapproval")
            {
                var preapprovalClient = new PreapprovalClient();
                var preapproval = await preapprovalClient.GetAsync(resourceId);

                if (preapproval == null) return;

                var membership = await _context.Memberships
                    .FirstOrDefaultAsync(m => m.MpPreapprovalId == resourceId);

                if (membership != null)
                {
                    if (preapproval.Status == "cancelled" || preapproval.Status == "paused")
                    {
                        membership.IsCancelled = true;
                    }
                    else if (preapproval.Status == "authorized")
                    {
                        membership.IsCancelled = false;
                    }

                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
