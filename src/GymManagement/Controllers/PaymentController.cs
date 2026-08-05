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

        public PaymentController(PaymentService paymentService, MercadoPagoService mercadoPagoService)
        {
            _paymentService = paymentService;
            _mercadoPagoService = mercadoPagoService;
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
        /// Supports both JSON payload and Query string notifications (Webhooks & IPN).
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> WebhookMercadoPago(
            [FromBody] MercadoPagoWebhookRequest? body,
            [FromQuery] string? type,
            [FromQuery(Name = "data.id")] string? dataId,
            [FromQuery] string? topic,
            [FromQuery] string? id)
        {
            // Determine resource type and resource ID from JSON body or Query String
            string? resourceType = body?.Type ?? body?.Topic ?? type ?? topic;
            string? resourceId = body?.Data?.Id ?? dataId ?? id ?? body?.Id?.ToString();

            if (!string.IsNullOrEmpty(resourceId))
            {
                await _mercadoPagoService.ProcessWebhookNotificationAsync(resourceType, resourceId);
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
