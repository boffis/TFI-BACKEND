using GymManagement.Domain.Entities;
using GymManagement.Infrastructure.Persistence;
using GymManagement.Infrastructure.Settings;
using GymManagement.Application.Exceptions;
using MercadoPago.Client.Preference;
using MercadoPago.Config;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace GymManagement.Infrastructure.Payments
{
    public class MercadoPagoService
    {
        private readonly MercadoPagoSettings _settings;
        private readonly ApplicationDbContext _context;

        public MercadoPagoService(IOptions<MercadoPagoSettings> settings, ApplicationDbContext context)
        {
            _settings = settings.Value;
            _context = context;
            MercadoPagoConfig.AccessToken = _settings.AccessToken;
        }

        public async Task<string> CreatePreference(Guid membershipId)
        {
            var membership = await _context.Memberships
                .Include(m => m.MembershipPlan)
                .FirstOrDefaultAsync(m => m.MembershipId == membershipId);

            if (membership == null)
            {
                throw new NotFoundException($"Membership {membershipId} no encontrada.");
            }

            if (membership.MembershipPlan == null)
            {
                throw new Exception("Membership does not have an associated plan.");
            }

            var price = membership.MembershipPlan.Price;

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
                    Success = "https://tugym.com/api/payments/success",
                    Failure = "https://tugym.com/api/payments/failure",
                    Pending = "https://tugym.com/api/payments/pending"
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
    }
}
