using GymManagement.Application.Requests;
using GymManagement.Application.Services;
using GymManagement.Domain.Entities;
using GymManagement.Infrastructure.Payments;
using GymManagement.Presentation.Authorization;
using GymManagement.Presentation.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagement.Presentation.Controllers
{
    [Route("[controller]")]
    [Authorize]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly PaymentService _paymentService;
        private readonly MercadoPagoService _mercadoPagoService;
        private readonly IServiceScopeFactory _scopeFactory;

        public PaymentController(
            PaymentService paymentService,
            MercadoPagoService mercadoPagoService,
            IServiceScopeFactory scopeFactory)
        {
            _paymentService = paymentService;
            _mercadoPagoService = mercadoPagoService;
            _scopeFactory = scopeFactory;
        }

        [HttpGet("GetAllPayments")]
        [Authorize(Policy = Policies.OnlyAdmin)]
        public List<Payment> Get() => _paymentService.GetAllPayments();

        [HttpGet("GetPaymentById/{PaymentId}")]
        [Authorize(Policy = Policies.OnlyAdmin)]
        public IActionResult GetPayment(Guid PaymentId)
        {
            var payment = _paymentService.GetClientPayment(PaymentId);
            if (payment == null) return NotFound();
            return Ok(payment);
        }

        /// <summary>
        /// Legacy: creates a Mercado Pago Preference (redirect flow). Kept for reference.
        /// </summary>
        [HttpPost("CreatePayment")]
        [Authorize(Policy = Policies.OnlyClient)]
        public async Task<IActionResult> CreatePayment(Guid membershipId)
        {
            var url = await _mercadoPagoService.CreatePreference(membershipId);
            return Ok(new { PaymentUrl = url });
        }

        /// <summary>
        /// Mercado Pago Webhook notification endpoint.
        /// Publicly accessible (no JWT required) so Mercado Pago can post notifications.
        /// Validates x-signature header if WebhookSecret is configured.
        /// Supports both JSON payload and Query string notifications (Webhooks & IPN).
        /// Returns 200 immediately to acknowledge receipt, then processes the notification
        /// in the background using a dedicated DI scope to avoid DbContext disposal issues.
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public IActionResult WebhookMercadoPago(
            [FromBody] MercadoPagoWebhookRequest? body,
            [FromQuery] string? type,
            [FromQuery(Name = "data.id")] string? dataId,
            [FromQuery] string? topic,
            [FromQuery] string? id)
        {
            string? resourceType = body?.Type ?? body?.Topic ?? type ?? topic;
            string? resourceId = body?.Data?.Id ?? dataId ?? id ?? body?.Id?.ToString();

            string? xSignature = Request.Headers["x-signature"].ToString();
            string? requestId = Request.Headers["x-request-id"].ToString();

            if (!string.IsNullOrEmpty(resourceId))
            {
                // Validate HMAC-SHA256 signature if WebhookSecret is configured
                if (!_mercadoPagoService.ValidateWebhookSignature(xSignature, requestId, resourceId))
                {
                    return Unauthorized("Firma de webhook inválida.");
                }

                // Fire-and-forget: process in background so MP gets 200 immediately.
                // A new DI scope is created for the background work so that the scoped
                // MercadoPagoService (and its DbContext) are not disposed with this request.
                _ = Task.Run(async () =>
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var svc = scope.ServiceProvider.GetRequiredService<MercadoPagoService>();
                    await svc.ProcessWebhookNotificationAsync(resourceType, resourceId);
                });
            }

            // Always respond HTTP 200 OK immediately to acknowledge receipt to Mercado Pago
            return Ok();
        }

        // ─── Subscription endpoints ───────────────────────────────────────────────────

        /// <summary>
        /// Creates a recurring subscription using the card token from the Card Payment Brick.
        /// The authenticated user's ID is extracted from their JWT. 
        /// Body must match the frontend's formData plus membershipPlanId.
        /// </summary>
        [HttpPost("Subscribe")]
        [Authorize(Policy = Policies.OnlyClient)]
        public async Task<IActionResult> Subscribe([FromBody] SubscriptionRequest request)
        {
            var userId = User.GetUserId();
            var result = await _mercadoPagoService.CreateSubscriptionAsync(request, userId);
            return Ok(result);
        }

        /// <summary>
        /// Cancels the user's active recurring subscription.
        /// Only the owner of the membership (verified via JWT) can cancel it.
        /// </summary>
        [HttpPost("Unsubscribe/{membershipId}")]
        [Authorize(Policy = Policies.OnlyClient)]
        public async Task<IActionResult> Unsubscribe(Guid membershipId)
        {
            var userId = User.GetUserId();
            await _mercadoPagoService.CancelSubscriptionAsync(membershipId, userId);
            return Ok(new { message = "Suscripción cancelada exitosamente." });
        }
    }
}
