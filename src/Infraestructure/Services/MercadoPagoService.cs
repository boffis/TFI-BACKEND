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

        /// <summary>Validates Mercado Pago's x-signature header (HMAC-SHA256). Bypassed when no secret is configured.</summary>
        public bool ValidateWebhookSignature(string? xSignature, string? requestId, string? dataId)
        {
            if (string.IsNullOrWhiteSpace(_settings.WebhookSecret))
            {
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

        // Legacy redirect-based flow, kept for reference.
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

        /// <summary>
        /// Charges the Card Payment Brick token, activates the membership and creates the
        /// recurring Preapproval for future automatic charges.
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

            // Before any preapproval exists: a stale pricing page must not subscribe to a retired plan.
            if (plan.IsDeleted)
                throw new ConflictException("This membership plan is no longer available.");

            // Read before the conflict check: it cancels expired memberships, hiding them from Step 2.
            var previousMembership = await _context.Memberships
                .FirstOrDefaultAsync(m => m.UserId == userId && !m.IsCancelled);

            // Before Step 1: throwing later would leave a live subscription charging a rejected client.
            await _membershipService.EnsureNoConflictingMembershipAsync(userId, selfService: true);

            // Step 1: create the preapproval over raw HTTP — SDK v3.3.0 lacks card_token_id.
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
                // notification_url is unsupported for preapprovals; set it in the MP dashboard.
                reason = $"Membresía {plan.Type} - Gym Management",
                external_reference = userId.ToString(),
                payer_email = request.Payer.Email,
                card_token_id = request.Token,
                status = "authorized",
                auto_recurring = new
                {
                    frequency = frequencyValue,
                    frequency_type = frequencyType,
                    // No start_date → MP charges the first installment within ~1 hour.
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

            // Step 2: an expired membership's subscription may still be live at MP; cancel it or
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

            // Step 3: record the membership and a pending payment. Always a new row — the conflict
            // check leaves nothing to reuse, and each subscription keeps its own history.
            var membership = new Membership
            {
                MembershipId = Guid.NewGuid(),
                UserId = userId,
                User = null!,
                MembershipPlanId = plan.MembershipPlanId,
                MembershipPlan = null!,
                IsCancelled = false,
                MpPreapprovalId = preapprovalId,
                // Provisional; the webhook resets it when MP confirms the payment.
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

            // Same payment shape login serves, so the client can append it to its cached user.
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

        /// <summary>Owner-only cancellation, in Mercado Pago and locally.</summary>
        public async Task CancelSubscriptionAsync(Guid membershipId, Guid userId)
        {
            var membership = await _context.Memberships
                .FirstOrDefaultAsync(m => m.MembershipId == membershipId)
                ?? throw new NotFoundException($"Membership {membershipId} not found.");

            if (membership.UserId != userId)
                throw new UnauthorizedException("You don't have permission to cancel this membership.");

            await CancelSubscriptionInternalAsync(membership);
        }

        /// <summary>Admin path: same cancellation as above, without the ownership check.</summary>
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

            // AutoRenew == false → already cancelled at MP (discontinued plan). Cancelling twice can
            // return a non-404 error and block the client, so go straight to the local cancellation.
            if (!string.IsNullOrEmpty(membership.MpPreapprovalId) && membership.AutoRenew)
            {
                var failure = await TryCancelPreapprovalAsync(membership.MpPreapprovalId);
                if (failure != null)
                {
                    _logger.LogError(
                        "[MercadoPago] Failed to cancel preapproval {PreapprovalId} — {Error}",
                        membership.MpPreapprovalId, failure);

                    // A plain Exception would reach the client as a generic error, hiding that
                    // Mercado Pago is the one failing and that retrying is worth doing.
                    throw new BillingUnavailableException("Mercado Pago couldn't cancel the subscription.");
                }
            }

            // Only after MP confirmed above.
            membership.IsCancelled = true;
            await _context.SaveChangesAsync();
            await RemoveFutureInscriptionsAsync(membership.UserId);
        }

        /// <summary>
        /// Stops future charges without touching the membership: IsCancelled stays false and access
        /// runs out at ExpirationDate. Best-effort — returns false instead of throwing, so one stale
        /// preapproval doesn't abort discontinuing the plan for everyone else.
        /// </summary>
        public async Task<bool> StopAutoRenewalAsync(string preapprovalId)
        {
            var failure = await TryCancelPreapprovalAsync(preapprovalId);
            if (failure == null) return true;

            _logger.LogWarning(
                "[MercadoPago] Failed to stop auto-renewal on preapproval {PreapprovalId} — {Error}",
                preapprovalId, failure);
            return false;
        }

        /// <summary>
        /// The one place that cancels a preapproval at Mercado Pago. Returns null on success or a
        /// description of the failure — callers decide whether it is fatal. Uses the named
        /// "MercadoPago" client (Polly retries) rather than the SDK, which skips that pipeline.
        /// A preapproval MP no longer has counts as success.
        /// </summary>
        private async Task<string?> TryCancelPreapprovalAsync(string preapprovalId)
        {
            try
            {
                var http = _httpClientFactory.CreateClient("MercadoPago");
                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.AccessToken?.Trim()}");

                var body = JsonSerializer.Serialize(new { status = "cancelled" });
                var content = new StringContent(body, Encoding.UTF8, "application/json");

                var response = await http.PutAsync(
                    $"https://api.mercadopago.com/preapproval/{preapprovalId}",
                    content);

                if (response.IsSuccessStatusCode) return null;

                var error = await response.Content.ReadAsStringAsync();

                bool resourceGone = response.StatusCode == System.Net.HttpStatusCode.NotFound
                    || error.Contains("resource not found", StringComparison.OrdinalIgnoreCase);

                if (resourceGone) return null;

                return $"HTTP {(int)response.StatusCode}: {error}";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /// <summary>
        /// Retires an expired membership being replaced, stopping its recurring charge first — its
        /// preapproval is usually still live at MP and would otherwise bill forever.
        /// </summary>
        public async Task RetireSupersededMembershipAsync(Membership membership)
        {
            if (membership.IsCancelled) return;

            // Nothing to stop at MP: cash membership, or the preapproval is already cancelled.
            if (string.IsNullOrWhiteSpace(membership.MpPreapprovalId) || !membership.AutoRenew)
            {
                membership.IsCancelled = true;
                await _context.SaveChangesAsync();
                return;
            }

            // Persisted *before* calling MP: the webhook reads AutoRenew to tell our own cancellation
            // apart from a client walking away, and would otherwise strip future inscriptions.
            membership.AutoRenew = false;
            await _context.SaveChangesAsync();

            var failure = await TryCancelPreapprovalAsync(membership.MpPreapprovalId);

            if (failure != null)
            {
                // Roll back, or a retry hits the "nothing to stop" guard and orphans the preapproval.
                membership.AutoRenew = true;
                await _context.SaveChangesAsync();

                _logger.LogError(
                    "[MercadoPago] Could not cancel preapproval {PreapprovalId} while retiring expired membership {MembershipId} — {Error}",
                    membership.MpPreapprovalId, membership.MembershipId, failure);

                throw new BillingUnavailableException(
                    "Mercado Pago couldn't cancel the previous subscription. Please try again.");
            }

            // Future inscriptions are left alone: a replacement membership is about to be created.
            membership.IsCancelled = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "[MercadoPago] Retired expired membership {MembershipId} and cancelled its preapproval {PreapprovalId}",
                membership.MembershipId, membership.MpPreapprovalId);
        }

        /// <summary>
        /// Repoints an existing preapproval at <paramref name="newAmount"/> for the next billing
        /// cycle. Best-effort, so one stale preapproval doesn't abort a plan-wide price update.
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
        /// Drops the client's not-yet-happened class bookings. Called whenever a membership becomes
        /// cancelled, so a client without access doesn't stay booked.
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

        /// <summary>
        /// Handles Mercado Pago webhooks: approved payments extend the membership, preapproval
        /// updates sync the local cancellation state.
        /// </summary>
        public async Task ProcessWebhookNotificationAsync(string? resourceType, string resourceId)
        {
            if (string.IsNullOrWhiteSpace(resourceId)) return;

            resourceType = resourceType?.ToLowerInvariant();

            // Fire-and-forget from a background Task.Run — the controller already returned 200, so an
            // unhandled exception would vanish silently with no retry from MP.
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

                // Idempotency: skip only if fully processed; a "pending" row falls through to update.
                var existingPayment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.MpPaymentId == resourceId);

                if (existingPayment != null && existingPayment.PaymentState == "approved")
                {
                    return;
                }

                var paymentClient = new PaymentClient();
                var mpPayment = await paymentClient.GetAsync(mpPaymentId);

                if (mpPayment == null) return;

                // $0 card-validation charges are not real payments.
                if (mpPayment.OperationType == "card_validation"
                    || (mpPayment.TransactionAmount.HasValue && mpPayment.TransactionAmount.Value == 0))
                {
                    return;
                }

                if (mpPayment.Status == "approved")
                {
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

                    // Auto-charged renewals often lack ExternalReference but carry the subscription id.
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

                    // Last resort: payer email → local user → active membership.
                    if (membership == null)
                    {
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
                        // A charge against a cancelled membership means an orphaned subscription is
                        // still billing a client with no access. The subscription_id fallback above
                        // deliberately doesn't filter those out, so they surface here.
                        if (membership.IsCancelled)
                        {
                            _logger.LogError(
                                "[MercadoPago Webhook] Approved payment {PaymentId} (amount {Amount}) charged CANCELLED membership {MembershipId} — preapproval {PreapprovalId} was never cancelled and is still billing the client",
                                resourceId, mpPayment.TransactionAmount, membership.MembershipId, membership.MpPreapprovalId);

                            // Self-heal so the client isn't billed again next cycle. No ExpirationDate
                            // extension: this charge buys nothing. The payment is still recorded below,
                            // since the money genuinely moved.
                            if (!string.IsNullOrWhiteSpace(membership.MpPreapprovalId))
                                await StopAutoRenewalAsync(membership.MpPreapprovalId);
                        }
                        else
                        {
                            // A charge landing after auto-renewal stopped is an in-flight straggler, not
                            // an orphan — the client paid for this period, so honour it.
                            if (!membership.AutoRenew)
                                _logger.LogWarning(
                                    "[MercadoPago Webhook] Approved payment {PaymentId} landed on membership {MembershipId} after auto-renewal was stopped — honouring it as an in-flight charge",
                                    resourceId, membership.MembershipId);

                            membership.ExpirationDate = DateTime.UtcNow > membership.ExpirationDate
                                ? DateTime.UtcNow.AddDays(membership.MembershipPlan.DurationInDays)
                                : membership.ExpirationDate.AddDays(membership.MembershipPlan.DurationInDays);
                        }

                        // Update the pending row created when the subscription started, falling back
                        // to the MpPaymentId match.
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
                    // CreateSubscriptionAsync grants access optimistically once the preapproval is
                    // authorized, so a denied charge must not leave the membership active. No
                    // payer-email fallback here: revoking access is destructive, so exact matches only.
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
                        // Only the initiating charge (still "pending") revokes access; a denied renewal
                        // on an already-paid membership is left alone.
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
                    // Revoke only, never re-grant: "authorized" means the agreement is valid, not that
                    // the last charge succeeded, so treating it as un-cancel would undo the revocation
                    // from a rejected-payment webhook when the two notifications race.
                    // AutoRenew == false means we cancelled this preapproval ourselves (discontinued
                    // plan) — the "cancelled" notification is our own echo, not the client leaving.
                    bool justCancelled = !membership.IsCancelled && membership.AutoRenew &&
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
