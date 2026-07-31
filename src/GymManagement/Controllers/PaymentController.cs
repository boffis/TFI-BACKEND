using GymManagement.Application.Services;
using GymManagement.Domain.Entities;
using GymManagement.Infrastructure.Payments;
using GymManagement.Presentation.Authorization;
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

        
        [HttpPost("CreatePayment")]
        [Authorize(Policy = Policies.OnlyClient)]
        public async Task<IActionResult> CreatePayment(Guid membershipId)
        {
            var url = await _mercadoPagoService.CreatePreference(membershipId);
            return Ok(new { PaymentUrl = url });
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> WebhookMercadoPago(Guid paymentId, string status)
        {
            await _mercadoPagoService.ProcessWebhook(paymentId, status);
            return Ok();
        }
    }
}
