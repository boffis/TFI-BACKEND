using GymManagement.Application.Interfaces;
using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Domain.Entities;
using GymManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<Payment> _dbSet;
        public PaymentRepository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Payments;
        }

        public List<Payment> GetAllPayments()
        {
            return [.. _dbSet];
        }

        public Payment? GetPaymentById(Guid paymentId)
        {
            return _dbSet.FirstOrDefault(p => p.PaymentId == paymentId);
        }

        public Payment AddPayment(Payment payment) 
        {
            _dbSet.Add(payment);
            _context.SaveChanges();
            return payment;
        }
    }
}
