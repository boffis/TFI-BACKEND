using GymManagement.Domain.Entities;

namespace GymManagement.Application.Interfaces
{
    public interface IPaymentRepository
    {
        List<Payment> GetAllPayments();
        
        Payment? GetPaymentById(Guid PaymentId);

        Payment AddPayment(Payment payment);
    }
}