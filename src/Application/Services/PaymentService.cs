using GymManagement.Application.Interfaces;
using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Domain.Entities;

namespace GymManagement.Application.Services
{
    public class PaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IClientRepository _clientRepository;
        private readonly IMembershipRepository _membershipRepository;

        public PaymentService(IPaymentRepository paymentRepository, IClientRepository clientRepository, IMembershipRepository membershipRepository)
        {
            _paymentRepository = paymentRepository;
            _clientRepository = clientRepository;
            _membershipRepository = membershipRepository;
        }

        public List<Payment> GetAllPayments() => _paymentRepository.GetAllPayments();

        public Payment? GetClientPayment(Guid paymentId) => _paymentRepository.GetPaymentById(paymentId);

        //public PaymentResponse? CreatePayment(Guid clientId, PaymentRequest request)
        //{
        //    var client = _clientRepository.GetById(clientId) ?? 
        //        throw new InvalidOperationException("Client not found");

        //    var membership = _membershipRepository.GetMembershipById(request.MembershipId) ??
        //        throw new InvalidOperationException("Membership not found");

        //    var newPayment = new Payment
        //    {
        //        PaymentId = Guid.NewGuid(),
        //        MembershipId = request.MembershipId,
        //        Membership = membership,
        //        Price = request.Price,
        //        PaymentDate = DateTime.UtcNow,
        //        PaymentMethod = "Mercado Pago",
        //        PaymentState = "Pending"
        //    };

        //    _paymentRepository.AddPayment(newPayment);

        //    return new PaymentResponse
        //    {
        //        PaymentId = newPayment.PaymentId,
        //        MembershipId = newPayment.MembershipId,
        //        Price = newPayment.Price,
        //        PaymentDate = newPayment.PaymentDate,
        //        PaymentMethod = newPayment.PaymentMethod
        //    };
        //}
    }
}
