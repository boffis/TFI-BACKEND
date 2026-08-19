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
        private readonly MembershipService _membershipService;
        private readonly IServiceScopeFactory _scopeFactory;

        public PaymentController(
            PaymentService paymentService,
            MercadoPagoService mercadoPagoService,
            MembershipService membershipService,
            IServiceScopeFactory scopeFactory)
        {
            _paymentService = paymentService;
            _mercadoPagoService = mercadoPagoService;
            _membershipService = membershipService;
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

        /// <summary>Cash membership: activates immediately and logs a "cash"/"approved" Payment.</summary>
        [HttpPost("GrantCashMembership")]
        [Authorize(Policy = Policies.OnlyAdmin)]
        public async Task<IActionResult> GrantCashMembership([FromBody] MembershipRequest request)
        {
            var response = await _membershipService.GrantCashMembershipAsync(request);
            return Ok(response);
        }

        /// <summary>Legacy Mercado Pago Preference (redirect flow). Kept for reference.</summary>
        [HttpPost("CreatePayment")]
        [Authorize(Policy = Policies.OnlyClient)]
        public async Task<IActionResult> CreatePayment(Guid membershipId)
        {
            var url = await _mercadoPagoService.CreatePreference(membershipId);
            return Ok(new { PaymentUrl = url });
        }

        /// <summary>
        /// Mercado Pago webhook endpoint — anonymous, signature-checked, accepting both JSON and
        /// query-string notifications. Returns 200 at once and processes in a background DI scope.
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
                if (!_mercadoPagoService.ValidateWebhookSignature(xSignature, requestId, resourceId))
                {
                    return Unauthorized("Invalid webhook signature.");
                }

                // Fire-and-forget in its own DI scope, so the scoped DbContext outlives this request.
                _ = Task.Run(async () =>
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var svc = scope.ServiceProvider.GetRequiredService<MercadoPagoService>();
                    await svc.ProcessWebhookNotificationAsync(resourceType, resourceId);
                });
            }

            return Ok();
        }

        /// <summary>
        /// Creates a recurring subscription from the Card Payment Brick token. Body is the
        /// frontend's formData plus membershipPlanId; the user comes from the JWT.
        /// </summary>
        [HttpPost("Subscribe")]
        [Authorize(Policy = Policies.OnlyClient)]
        public async Task<IActionResult> Subscribe([FromBody] SubscriptionRequest request)
        {
            var userId = User.GetUserId();
            var result = await _mercadoPagoService.CreateSubscriptionAsync(request, userId);
            return Ok(result);
        }

        /// <summary>Cancels the caller's own recurring subscription (ownership verified via JWT).</summary>
        [HttpPost("Unsubscribe/{membershipId}")]
        [Authorize(Policy = Policies.OnlyClient)]
        public async Task<IActionResult> Unsubscribe(Guid membershipId)
        {
            var userId = User.GetUserId();
            await _mercadoPagoService.CancelSubscriptionAsync(membershipId, userId);
            return Ok(new { message = "Suscripción cancelada exitosamente." });
        }

        /// <summary>Admin: revokes any client's membership and cancels its MP subscription.</summary>
        [HttpPost("AdminRevokeMembership/{membershipId}")]
        [Authorize(Policy = Policies.OnlyAdmin)]
        public async Task<IActionResult> AdminRevokeMembership(Guid membershipId)
        {
            await _mercadoPagoService.AdminCancelSubscriptionAsync(membershipId);
            return Ok(new { message = "Membresía revocada exitosamente." });
        }
    }
}
