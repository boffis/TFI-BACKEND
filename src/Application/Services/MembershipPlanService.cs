using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GymManagement.Application.Interfaces;
using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Domain.Entities;
using GymManagement.Application.Exceptions;
using Microsoft.Extensions.Logging;

namespace GymManagement.Application.Services
{
    public class MembershipPlanService : IMembershipPlanService
    {
        private readonly IMembershipPlanRepository _membershipPlanRepository;
        private readonly IMembershipRepository _membershipRepository;
        private readonly IMembershipBillingService _billingService;
        private readonly IEmailService _emailService;
        private readonly ILogger<MembershipPlanService> _logger;

        public MembershipPlanService(
            IMembershipPlanRepository membershipPlanRepository,
            IMembershipRepository membershipRepository,
            IMembershipBillingService billingService,
            IEmailService emailService,
            ILogger<MembershipPlanService> logger)
        {
            _membershipPlanRepository = membershipPlanRepository;
            _membershipRepository = membershipRepository;
            _billingService = billingService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<IEnumerable<MembershipPlanResponse>> GetAllPlansAsync()
        {
            var plans = await _membershipPlanRepository.GetAllAsync();
            return plans.Select(p => new MembershipPlanResponse
            {
                MembershipPlanId = p.MembershipPlanId,
                Type = p.Type,
                Price = p.Price,
                DurationInDays = p.DurationInDays
            });
        }

        public async Task<MembershipPlanResponse> GetPlanByIdAsync(Guid id)
        {
            var plan = await _membershipPlanRepository.GetByIdAsync(id);
            if (plan == null)
                throw new NotFoundException($"Membership plan {id} not found.");

            return new MembershipPlanResponse
            {
                MembershipPlanId = plan.MembershipPlanId,
                Type = plan.Type,
                Price = plan.Price,
                DurationInDays = plan.DurationInDays
            };
        }

        public async Task<AdminMembershipPlanResponse> GetAdminPlanByIdAsync(Guid id)
        {
            var plan = await _membershipPlanRepository.GetByIdAsync(id);
            if (plan == null)
                throw new NotFoundException($"Membership plan {id} not found.");

            var memberships = await _membershipRepository.GetByPlanId(id);

            return new AdminMembershipPlanResponse
            {
                MembershipPlanId = plan.MembershipPlanId,
                Type = plan.Type,
                Price = plan.Price,
                DurationInDays = plan.DurationInDays,
                Memberships = memberships.Select(m => new MembershipSummaryResponse
                {
                    MembershipId = m.MembershipId,
                    ClientId = m.UserId,
                    ClientName = m.User.Name,
                    ClientEmail = m.User.Email,
                    IsCancelled = m.IsCancelled,
                    ExpirationDate = m.ExpirationDate
                }).ToList()
            };
        }

        public async Task<MembershipPlanResponse> CreatePlanAsync(MembershipPlanRequest request)
        {
            var plan = new MembershipPlan
            {
                MembershipPlanId = Guid.NewGuid(),
                Type = request.Type,
                Price = request.Price,
                DurationInDays = request.DurationInDays
            };

            await _membershipPlanRepository.AddAsync(plan);

            return new MembershipPlanResponse
            {
                MembershipPlanId = plan.MembershipPlanId,
                Type = plan.Type,
                Price = plan.Price,
                DurationInDays = plan.DurationInDays
            };
        }

        public async Task<MembershipPlanResponse> UpdatePlanAsync(Guid id, MembershipPlanRequest request)
        {
            var plan = await _membershipPlanRepository.GetByIdAsync(id);
            if (plan == null)
                throw new NotFoundException($"Membership plan {id} not found.");

            var previousPrice = plan.Price;

            plan.Type = request.Type;
            plan.Price = request.Price;
            plan.DurationInDays = request.DurationInDays;

            await _membershipPlanRepository.UpdateAsync(plan);

            if (previousPrice != plan.Price)
            {
                await SyncPriceChangeToSubscribersAsync(plan, previousPrice);
            }

            return new MembershipPlanResponse
            {
                MembershipPlanId = plan.MembershipPlanId,
                Type = plan.Type,
                Price = plan.Price,
                DurationInDays = plan.DurationInDays
            };
        }

        /// <summary>
        /// When a plan's price changes, pushes the new amount to Mercado Pago for every
        /// active (non-cancelled) subscriber's preapproval so their next charge reflects it,
        /// and emails each of them a heads-up. Both steps are best-effort per subscriber —
        /// one failure doesn't stop the rest, and neither blocks the price change itself.
        /// </summary>
        private async Task SyncPriceChangeToSubscribersAsync(MembershipPlan plan, decimal previousPrice)
        {
            var memberships = await _membershipRepository.GetByPlanId(plan.MembershipPlanId);
            var activeSubscribers = memberships.Where(m => !m.IsCancelled).ToList();

            foreach (var membership in activeSubscribers)
            {
                if (!string.IsNullOrWhiteSpace(membership.MpPreapprovalId))
                {
                    await _billingService.UpdatePreapprovalAmountAsync(membership.MpPreapprovalId, plan.Price);
                }

                try
                {
                    string subject = $"Actualización de precio en tu membresía {plan.Type}";
                    string body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                        <h2 style='color: #2b2b2b;'>Actualización de precio</h2>
                        <p>Hola <strong>{membership.User.Name}</strong>,</p>
                        <p>El precio de tu membresía <strong>{plan.Type}</strong> cambió de ${previousPrice:F2} a <strong>${plan.Price:F2}</strong>.</p>
                        <p>Este nuevo precio se aplicará a partir de tu próximo cobro.</p>
                        <p style='color: #666; font-size: 13px;'>Si tenés alguna duda, podés contactarnos respondiendo a este correo.</p>
                    </div>";

                    await _emailService.SendEmailAsync(membership.User.Email, subject, body);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to send price-change email to {Email} for membership {MembershipId}",
                        membership.User.Email, membership.MembershipId);
                }
            }
        }

        public async Task DeletePlanAsync(Guid id)
        {
            var plan = await _membershipPlanRepository.GetByIdAsync(id);
            if (plan == null)
                throw new NotFoundException($"Membership plan {id} not found.");

            await _membershipPlanRepository.DeleteAsync(id);
        }
    }
}
