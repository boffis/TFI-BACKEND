using GymManagement.Application.Interfaces;
using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Domain.Entities;
using GymManagement.Application.Exceptions;

namespace GymManagement.Application.Services
{
    public class PaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IMembershipRepository _membershipRepository;

        public PaymentService(IPaymentRepository paymentRepository, IMembershipRepository membershipRepository)
        {
            _paymentRepository = paymentRepository;
            _membershipRepository = membershipRepository;
        }

        public List<Payment> GetAllPayments() => _paymentRepository.GetAllPayments();

        public Payment? GetClientPayment(Guid paymentId) => _paymentRepository.GetPaymentById(paymentId);

        public async Task<PaymentResponse> CreatePayment(PaymentRequest request)
        {
            var membership = await _membershipRepository.GetMembershipById(request.MembershipId)
                ?? throw new ConflictException("Membership not found");

            var newPayment = new Payment
            {
                PaymentId = Guid.NewGuid(),
                UserId = membership.UserId,
                User = membership.User,
                MembershipId = request.MembershipId,
                Membership = membership,
                Price = request.Price,
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = "Mercado Pago",
                PaymentState = "Pending"
            };

            _paymentRepository.AddPayment(newPayment);

            return new PaymentResponse
            {
                PaymentId = newPayment.PaymentId,
                UserId = newPayment.UserId,
                MembershipId = newPayment.MembershipId,
                Price = newPayment.Price,
                PaymentDate = newPayment.PaymentDate,
                PaymentMethod = newPayment.PaymentMethod
            };
        }
    }
}
