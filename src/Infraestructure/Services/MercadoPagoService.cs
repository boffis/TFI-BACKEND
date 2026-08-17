using GymManagement.Application.Common;
using GymManagement.Application.Exceptions;
using GymManagement.Application.Interfaces;
using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Application.Services;
using GymManagement.Domain.Entities;
using GymManagement.Infrastructure.Persistence;
using GymManagement.Infrastructure.Settings;
using MercadoPago.Client.Common;
using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;
using MercadoPago.Client.Preapproval;
using MercadoPago.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    public class MercadoPagoService : IMembershipBillingService
    {
        private readonly MercadoPagoSettings _settings;
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MercadoPagoService> _logger;
        private readonly MembershipService _membershipService;

        public MercadoPagoService(
            IOptions<MercadoPagoSettings> settings,
            ApplicationDbContext context,
            Microsoft.Extensions.Configuration.IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<MercadoPagoService> logger,
            MembershipService membershipService)
        {
            _settings = settings.Value;
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _membershipService = membershipService;
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
                throw new NotFoundException($"Membership {membershipId} not found.");

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
                throw new ValidationException("The 'payment_method_id' field is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Token))
            {
                throw new ValidationException("The card 'token' field is required.");
            }

            var plan = await _context.MembershipPlans
                .FirstOrDefaultAsync(p => p.MembershipPlanId == request.MembershipPlanId)
                ?? throw new NotFoundException($"Plan {request.MembershipPlanId} not found.");

            // Read the client's current membership before the conflict check runs: the check
            // cancels an expired one, which would hide it from the Step 2 lookup below and leave
            // its Mercado Pago subscription billing the card forever.
            var previousMembership = await _context.Memberships
                .FirstOrDefaultAsync(m => m.UserId == userId && !m.IsCancelled);

            // One membership per client, refused here rather than after Step 1: throwing once the
            // preapproval exists would leave a live subscription charging a client we just said
            // "no" to. A still-valid membership throws ConflictException (HTTP 409) and the client
            // is told to cancel it first; an expired one is cancelled to make room.
            await _membershipService.EnsureNoConflictingMembershipAsync(userId, selfService: true);

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
                // notification_url is NOT supported for preapprovals — configure it
                // in the Mercado Pago Developer Dashboard instead.
                reason = $"Membresía {plan.Type} - Gym Management",
                external_reference = userId.ToString(),
                payer_email = request.Payer.Email,
                card_token_id = request.Token,
                status = "authorized",
                auto_recurring = new
                {
                    frequency = frequencyValue,
                    frequency_type = frequencyType,
                    // No start_date → MP charges the first installment immediately
                    // (within ~1 hour of subscription creation).
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
                {
                    _logger.LogError(
                        "[MercadoPago] Failed to create preapproval (HTTP {StatusCode}): {Body}",
                        (int)response.StatusCode, responseBody);
                    throw new ValidationException("We couldn't set up your subscription with the payment provider. Please check your card details and try again.");
                }

                using var doc = JsonDocument.Parse(responseBody);
                preapprovalId = doc.RootElement.GetProperty("id").GetString();
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MercadoPago] Unexpected error creating subscription for user {UserId}", userId);
                throw new ValidationException("We couldn't set up your subscription. Please try again or contact support.");
            }

            // ── Step 2: Cancel the previous preapproval in MP to avoid duplicates ────
            // Only an expired membership can still be here — a valid one threw above — but its
            // subscription may well be alive at Mercado Pago (that is how a membership expires
            // without being cancelled: the renewal charge stopped going through). Cancel it, or
            // the card gets charged for both the old plan and the new one.
            if (previousMembership != null && !string.IsNullOrEmpty(previousMembership.MpPreapprovalId))
            {
                try
                {
                    var cancelHttp = _httpClientFactory.CreateClient();
                    cancelHttp.DefaultRequestHeaders.Clear();
                    cancelHttp.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.AccessToken?.Trim()}");
                    var cancelBody = JsonSerializer.Serialize(new { status = "cancelled" });
                    var cancelContent = new StringContent(cancelBody, Encoding.UTF8, "application/json");
                    await cancelHttp.PutAsync(
                        $"https://api.mercadopago.com/preapproval/{previousMembership.MpPreapprovalId}",
                        cancelContent);
                }
                catch { /* best-effort: if old preapproval is already gone, ignore */ }
            }

            // ── Step 3: Activate the membership locally ────────────────────────────
            // MP automatically charges the card within 1 hour of subscription creation.
            // We record the membership and a pending payment locally.
            // Always a new row: the conflict check leaves no non-cancelled membership behind, so
            // there is nothing to reuse, and each subscription keeps its own history — same as the
            // admin paths in MembershipService.
            var membership = new Membership
            {
                MembershipId = Guid.NewGuid(),
                UserId = userId,
                User = null!,
                MembershipPlanId = plan.MembershipPlanId,
                MembershipPlan = null!,
                IsCancelled = false,
                MpPreapprovalId = preapprovalId,
                // ExpirationDate will be updated by webhook when MP confirms the payment.
                // Set a provisional expiration based on plan duration.
                ExpirationDate = DateTime.UtcNow.AddDays(plan.DurationInDays)
            };

            _context.Memberships.Add(membership);

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

            // The payment is returned in the same shape login serves it (UserService:120), so the
            // client can append it to its cached user without a round trip and end up with exactly
            // what it would get after signing out and back in.
            return new
            {
                preapprovalId = preapprovalId,
                paymentStatus = "pending",
                membershipId = membership.MembershipId,
                expirationDate = membership.ExpirationDate,
                payment = new PaymentResponse
                {
                    PaymentId = localPayment.PaymentId,
                    UserId = localPayment.UserId,
                    MembershipId = localPayment.MembershipId,
                    Price = localPayment.Price,
                    PaymentDate = localPayment.PaymentDate,
                    PaymentMethod = localPayment.PaymentMethod,
                    PaymentState = localPayment.PaymentState
                }
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
                ?? throw new NotFoundException($"Membership {membershipId} not found.");

            if (membership.UserId != userId)
                throw new UnauthorizedException("You don't have permission to cancel this membership.");

            await CancelSubscriptionInternalAsync(membership);
        }

        /// <summary>
        /// Admin-only path: revokes any client's membership regardless of ownership. Shares the
        /// same Mercado Pago preapproval cancellation as the client self-service flow above, so a
        /// revoked MP-billed membership actually stops billing instead of just flipping a flag here.
        /// </summary>
        public async Task AdminCancelSubscriptionAsync(Guid membershipId)
        {
            var membership = await _context.Memberships
                .FirstOrDefaultAsync(m => m.MembershipId == membershipId)
                ?? throw new NotFoundException($"Membership {membershipId} not found.");

            await CancelSubscriptionInternalAsync(membership);
        }

        private async Task CancelSubscriptionInternalAsync(Membership membership)
        {
            if (membership.IsCancelled)
                throw new ConflictException("This membership is already cancelled.");

            if (!string.IsNullOrEmpty(membership.MpPreapprovalId))
            {
                // Use the named HttpClient which carries the Polly retry policy.
                // We call the MP REST API directly (PUT /preapproval/{id}) because the SDK
                // does not support the retry pipeline. On any non-2xx after 5 attempts, this throws.
                var http = _httpClientFactory.CreateClient("MercadoPago");
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.AccessToken?.Trim()}");

                var body = JsonSerializer.Serialize(new { status = "cancelled" });
                var content = new StringContent(body, Encoding.UTF8, "application/json");

                var response = await http.PutAsync(
                    $"https://api.mercadopago.com/preapproval/{membership.MpPreapprovalId}",
                    content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();

                    // MP may return 404 or sometimes a 400/other code with "resource not found" in the body
                    // when the preapproval no longer exists. In both cases, nothing is left to cancel on MP's
                    // side so we treat it as success and proceed with local cancellation.
                    bool resourceGone = response.StatusCode == System.Net.HttpStatusCode.NotFound
                        || error.Contains("resource not found", StringComparison.OrdinalIgnoreCase);

                    if (!resourceGone)
                    {
                        _logger.LogError(
                            "[MercadoPago] Failed to cancel preapproval {PreapprovalId} (HTTP {StatusCode}): {Error}",
                            membership.MpPreapprovalId, (int)response.StatusCode, error);
                        throw new Exception("Mercado Pago couldn't cancel the subscription.");
                    }
                }
            }

            // Only mark as cancelled locally after MP confirmed the cancellation above.
            membership.IsCancelled = true;
            await _context.SaveChangesAsync();
            await RemoveFutureInscriptionsAsync(membership.UserId);
        }

        /// <summary>
        /// Updates the recurring charge amount of an existing preapproval so the next
        /// billing cycle charges <paramref name="newAmount"/> instead of the old price.
        /// Best-effort: logs and returns false instead of throwing, so a plan-wide price
        /// update isn't aborted by one subscriber's preapproval being stale/gone on MP's side.
        /// </summary>
        public async Task<bool> UpdatePreapprovalAmountAsync(string preapprovalId, decimal newAmount)
        {
            try
            {
                var http = _httpClientFactory.CreateClient("MercadoPago");
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.AccessToken?.Trim()}");

                var body = JsonSerializer.Serialize(new
                {
                    auto_recurring = new { transaction_amount = newAmount }
                });
                var content = new StringContent(body, Encoding.UTF8, "application/json");

                var response = await http.PutAsync(
                    $"https://api.mercadopago.com/preapproval/{preapprovalId}",
                    content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning(
                        "[MercadoPago] Failed to update preapproval {PreapprovalId} amount to {NewAmount} (HTTP {StatusCode}): {Error}",
                        preapprovalId, newAmount, (int)response.StatusCode, error);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[MercadoPago] Error updating preapproval {PreapprovalId} amount to {NewAmount}",
                    preapprovalId, newAmount);
                return false;
            }
        }

        /// <summary>
        /// Removes a client's inscriptions to gym classes that haven't happened yet.
        /// Called whenever a membership becomes cancelled (self-service cancel, declined
        /// payment, or a Mercado Pago preapproval cancellation) so a client without an
        /// active membership doesn't stay booked into future classes.
        /// </summary>
        private async Task RemoveFutureInscriptionsAsync(Guid userId)
        {
            var futureInscriptions = await _context.Inscriptions
                .Include(i => i.GymClass)
                .Where(i => i.ClientId == userId && i.GymClass.Schedule >= GymTime.Now)
                .ToListAsync();

            if (futureInscriptions.Count == 0) return;

            _context.Inscriptions.RemoveRange(futureInscriptions);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "[MercadoPago] Removed {Count} future inscription(s) for user {UserId} after membership cancellation",
                futureInscriptions.Count, userId);
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

            // This method runs fire-and-forget from a background Task.Run (the controller
            // already returned 200 to Mercado Pago by the time this executes), so an
            // unhandled exception here would otherwise vanish silently — no retry from MP,
            // no error anywhere. Log it so failures are actually visible in App Service logs.
            try
            {
                await ProcessWebhookNotificationCoreAsync(resourceType, resourceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[MercadoPago Webhook] Unhandled error processing {ResourceType} {ResourceId}",
                    resourceType, resourceId);
            }
        }

        private async Task ProcessWebhookNotificationCoreAsync(string? resourceType, string resourceId)
        {
            _logger.LogInformation(
                "[MercadoPago Webhook] Received {ResourceType} notification for resource {ResourceId}",
                resourceType, resourceId);

            if (resourceType == "payment" || resourceType == "subscription_authorized_payment")
            {
                if (!long.TryParse(resourceId, out long mpPaymentId)) return;

                // Idempotency check: skip only if this payment was already fully processed.
                // If it exists but is still "pending", fall through so its status gets updated.
                var existingPayment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.MpPaymentId == resourceId);

                if (existingPayment != null && existingPayment.PaymentState == "approved")
                {
                    // Already processed — nothing to do.
                    return;
                }

                // Fetch real-time payment status directly from Mercado Pago SDK
                var paymentClient = new PaymentClient();
                var mpPayment = await paymentClient.GetAsync(mpPaymentId);

                if (mpPayment == null) return;

                // Skip $0 card-validation charges — they are not real payments.
                if (mpPayment.OperationType == "card_validation"
                    || (mpPayment.TransactionAmount.HasValue && mpPayment.TransactionAmount.Value == 0))
                {
                    return;
                }

                if (mpPayment.Status == "approved")
                {
                    // Try to find the membership:
                    // 1) Via ExternalReference on the payment (userId)
                    // 2) Fallback: via the preapproval ID stored on the membership
                    Membership? membership = null;

                    if (Guid.TryParse(mpPayment.ExternalReference, out Guid userId))
                    {
                        membership = await _context.Memberships
                            .Include(m => m.MembershipPlan)
                            .FirstOrDefaultAsync(m => m.UserId == userId && !m.IsCancelled);

                        if (membership != null)
                            _logger.LogInformation(
                                "[MercadoPago Webhook] Payment {PaymentId} matched membership {MembershipId} via ExternalReference",
                                resourceId, membership.MembershipId);
                    }

                    // Fallback: recurring/auto-charged subscription payments often don't carry
                    // ExternalReference. Mercado Pago attaches the preapproval (subscription) id
                    // to the payment itself, so match it against the membership's MpPreapprovalId.
                    if (membership == null)
                    {
                        var subscriptionId = mpPayment.PointOfInteraction?.TransactionData?.SubscriptionId;
                        if (!string.IsNullOrWhiteSpace(subscriptionId))
                        {
                            membership = await _context.Memberships
                                .Include(m => m.MembershipPlan)
                                .FirstOrDefaultAsync(m => m.MpPreapprovalId == subscriptionId);

                            if (membership != null)
                                _logger.LogInformation(
                                    "[MercadoPago Webhook] Payment {PaymentId} matched membership {MembershipId} via subscription_id {SubscriptionId}",
                                    resourceId, membership.MembershipId, subscriptionId);
                        }
                    }

                    // Last-resort fallback: look up by payer email → user → membership.
                    if (membership == null)
                    {
                        // Try to find by payer email → local user → active membership
                        var payerEmail = mpPayment.Payer?.Email;
                        if (!string.IsNullOrWhiteSpace(payerEmail))
                        {
                            var user = await _context.Clients
                                .FirstOrDefaultAsync(u => u.Email == payerEmail);
                            if (user != null)
                            {
                                membership = await _context.Memberships
                                    .Include(m => m.MembershipPlan)
                                    .FirstOrDefaultAsync(m => m.UserId == user.UserId && !m.IsCancelled);

                                if (membership != null)
                                    _logger.LogWarning(
                                        "[MercadoPago Webhook] Payment {PaymentId} matched membership {MembershipId} via payer email fallback — ExternalReference and subscription_id both missed",
                                        resourceId, membership.MembershipId);
                            }
                        }
                    }

                    if (membership == null)
                    {
                        _logger.LogError(
                            "[MercadoPago Webhook] Approved payment {PaymentId} (amount {Amount}) could not be matched to any membership — money was collected but no local record will reflect it",
                            resourceId, mpPayment.TransactionAmount);
                    }

                    if (membership != null && membership.MembershipPlan != null)
                    {
                        // Extend membership expiration
                        membership.ExpirationDate = DateTime.UtcNow > membership.ExpirationDate
                            ? DateTime.UtcNow.AddDays(membership.MembershipPlan.DurationInDays)
                            : membership.ExpirationDate.AddDays(membership.MembershipPlan.DurationInDays);

                        // Try to find the existing pending payment for this membership
                        // (created locally when the subscription was initiated) and update it.
                        // Also fall back to existingPayment (matched by MpPaymentId) if no
                        // membership-scoped pending record is found.
                        var pendingPayment = await _context.Payments
                            .FirstOrDefaultAsync(p =>
                                p.MembershipId == membership.MembershipId &&
                                p.PaymentState == "pending")
                            ?? existingPayment;

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
                            // No existing record found at all: insert a new payment
                            var payment = new Payment
                            {
                                PaymentId = Guid.NewGuid(),
                                UserId = membership.UserId,
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
                else if (mpPayment.Status == "rejected" || mpPayment.Status == "cancelled")
                {
                    // The charge was denied. CreateSubscriptionAsync grants membership access
                    // optimistically as soon as the preapproval is authorized, before Mercado Pago
                    // actually attempts the real charge — if that charge is then denied, the
                    // membership must not be left silently active as if it had been paid.
                    // Only ExternalReference / subscription id are used here (not the payer-email
                    // fallback): revoking access is destructive, so we only act on precise matches.
                    Membership? membership = null;

                    if (Guid.TryParse(mpPayment.ExternalReference, out Guid userId))
                    {
                        membership = await _context.Memberships
                            .FirstOrDefaultAsync(m => m.UserId == userId && !m.IsCancelled);
                    }

                    if (membership == null)
                    {
                        var subscriptionId = mpPayment.PointOfInteraction?.TransactionData?.SubscriptionId;
                        if (!string.IsNullOrWhiteSpace(subscriptionId))
                        {
                            membership = await _context.Memberships
                                .FirstOrDefaultAsync(m => m.MpPreapprovalId == subscriptionId && !m.IsCancelled);
                        }
                    }

                    if (membership != null)
                    {
                        // Only revoke if this was the membership's initiating charge (still
                        // "pending" locally) — it was never actually paid for. A denied renewal
                        // charge on an already-paid membership is a separate concern and is left
                        // alone here.
                        var pendingPayment = await _context.Payments
                            .FirstOrDefaultAsync(p =>
                                p.MembershipId == membership.MembershipId &&
                                p.PaymentState == "pending");

                        if (pendingPayment != null)
                        {
                            pendingPayment.PaymentState = mpPayment.Status;
                            pendingPayment.MpPaymentId = resourceId;
                            membership.IsCancelled = true;

                            await _context.SaveChangesAsync();
                            await RemoveFutureInscriptionsAsync(membership.UserId);

                            _logger.LogWarning(
                                "[MercadoPago Webhook] Payment {PaymentId} for membership {MembershipId} was {Status} — membership access revoked",
                                resourceId, membership.MembershipId, mpPayment.Status);
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
                    // Only ever revoke here, never re-grant: an "authorized" preapproval just means
                    // the recurring billing agreement is still valid, NOT that the most recent charge
                    // succeeded — Mercado Pago keeps retrying a preapproval even after a declined
                    // charge, so treating "authorized" as "un-cancel" would undo the revocation from
                    // a rejected-payment webhook (see the payment.status == rejected/cancelled branch
                    // above) whenever the two notifications race. There is no reactivate/resume
                    // feature in this app that depends on the opposite transition.
                    bool justCancelled = !membership.IsCancelled &&
                        (preapproval.Status == "cancelled" || preapproval.Status == "paused");

                    if (justCancelled)
                    {
                        membership.IsCancelled = true;
                    }

                    await _context.SaveChangesAsync();

                    if (justCancelled)
                    {
                        await RemoveFutureInscriptionsAsync(membership.UserId);
                    }

                    _logger.LogInformation(
                        "[MercadoPago Webhook] Preapproval {PreapprovalId} status {Status} synced to membership {MembershipId} (IsCancelled={IsCancelled})",
                        resourceId, preapproval.Status, membership.MembershipId, membership.IsCancelled);
                }
                else
                {
                    _logger.LogWarning(
                        "[MercadoPago Webhook] Preapproval {PreapprovalId} status {Status} — no membership found with matching MpPreapprovalId",
                        resourceId, preapproval.Status);
                }
            }
        }
    }
}
