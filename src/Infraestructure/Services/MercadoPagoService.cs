using GymManagement.Domain.Entities;
using GymManagement.Domain.Enums;
using GymManagement.Infrastructure.Persistence;
using GymManagement.Infrastructure.Settings;
using MercadoPago.Client.Preference;
using MercadoPago.Config;
using Microsoft.Extensions.Options;

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

        private decimal GetMembershipPrice(MembershipType type)
        {
            return type switch
            {
                MembershipType.Weekly => 5000m,
                MembershipType.Monthly => 10000m,
                MembershipType.Quarterly => 25000m,
                MembershipType.Annual => 90000m,
                _ => 0m
            };
        }

        private int GetDurationDays(MembershipType type)
        {
            return type switch
            {
                MembershipType.Weekly => 7,
                MembershipType.Monthly => 30,
                MembershipType.Quarterly => 90,
                MembershipType.Annual => 365,
                _ => 0
            };
        }

        public async Task<string> CreatePreference(Guid membershipId, MembershipType membershipType)
        {
            var membership = await _context.Memberships.FindAsync(membershipId);
            if (membership == null)
            {
                throw new InvalidOperationException($"Membership {membershipId} no encontrada.");
            }

            var price = GetMembershipPrice(membershipType);

            var request = new PreferenceRequest
            {
                Items = new List<PreferenceItemRequest>
                {
                    new PreferenceItemRequest
                    {
                        Title = $"Membresía {membershipType}",
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
                var membership = await _context.Memberships.FindAsync(payment.MembershipId);
                if (membership != null)
                {
                    membership.MembershipState = true;
                    var durationDays = GetDurationDays(membership.MembershipType);
                    membership.ExpirationDate = DateTime.UtcNow.AddDays(durationDays);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}