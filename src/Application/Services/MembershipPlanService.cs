using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GymManagement.Application.Interfaces;
using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Domain.Entities;
using GymManagement.Application.Common;
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
            return plans.Select(ToResponse);
        }

        /// <summary>Admin listing: includes discontinued plans, flagged as such.</summary>
        public async Task<IEnumerable<MembershipPlanResponse>> GetAllPlansForAdminAsync()
        {
            var plans = await _membershipPlanRepository.GetAllIncludingDiscontinuedAsync();
            return plans.Select(ToResponse);
        }

        private static MembershipPlanResponse ToResponse(MembershipPlan p) => new()
        {
            MembershipPlanId = p.MembershipPlanId,
            Type = p.Type,
            Price = p.Price,
            DurationInDays = p.DurationInDays,
            IsDeleted = p.IsDeleted
        };

        public async Task<MembershipPlanResponse> GetPlanByIdAsync(Guid id)
        {
            var plan = await _membershipPlanRepository.GetByIdAsync(id);

            // Anonymous endpoint: a discontinued plan is gone, not flagged. Admins use GetAdminPlanByIdAsync.
            if (plan == null || plan.IsDeleted)
                throw new NotFoundException($"Membership plan {id} not found.");

            return ToResponse(plan);
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
                IsDeleted = plan.IsDeleted,
                Memberships = memberships.Select(m => new MembershipSummaryResponse
                {
                    MembershipId = m.MembershipId,
                    ClientId = m.UserId,
                    ClientName = m.User.Name,
                    ClientEmail = m.User.Email,
                    IsCancelled = m.IsCancelled,
                    AutoRenew = m.AutoRenew,
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

            return ToResponse(plan);
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

            return ToResponse(plan);
        }

        /// <summary>
        /// Pushes a new price to every active subscriber's preapproval and emails them. Best-effort
        /// per subscriber — one failure stops neither the rest nor the price change itself.
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

        /// <summary>
        /// Discontinues a plan: flagged, never deleted, since Memberships restricts the FK and the
        /// payment history depends on it. Existing subscribers stay active — their recurring charge
        /// is stopped and their membership runs out at its own expiration date.
        /// </summary>
        public async Task DeletePlanAsync(Guid id)
        {
            var plan = await _membershipPlanRepository.GetByIdAsync(id);
            if (plan == null)
                throw new NotFoundException($"Membership plan {id} not found.");

            if (plan.IsDeleted)
                throw new ConflictException("This membership plan is already discontinued.");

            await _membershipPlanRepository.DeleteAsync(id);

            await StopRenewalsForSubscribersAsync(plan);
        }

        /// <summary>
        /// Puts a discontinued plan back on sale. Nothing is restored on the billing side: cancelled
        /// preapprovals can't be reactivated, so old subscribers keep AutoRenew false and must
        /// subscribe again once their paid period runs out.
        /// </summary>
        public async Task RestorePlanAsync(Guid id)
        {
            var plan = await _membershipPlanRepository.GetByIdAsync(id);
            if (plan == null)
                throw new NotFoundException($"Membership plan {id} not found.");

            if (!plan.IsDeleted)
                throw new ConflictException("This membership plan is not discontinued.");

            await _membershipPlanRepository.RestoreAsync(id);
        }

        /// <summary>
        /// Stops the recurring charge for holders of a discontinued plan without revoking anything:
        /// IsCancelled stays false, so access survives to ExpirationDate. Best-effort per subscriber.
        /// </summary>
        private async Task StopRenewalsForSubscribersAsync(MembershipPlan plan)
        {
            var memberships = await _membershipRepository.GetByPlanId(plan.MembershipPlanId);

            // Expired-but-not-cancelled still counts: its preapproval would charge the card again.
            var subscribers = memberships.Where(m => !m.IsCancelled).ToList();
            if (subscribers.Count == 0) return;

            foreach (var membership in subscribers)
            {
                if (!membership.AutoRenew) continue;

                // Persisted *before* calling MP: the webhook reads AutoRenew to tell our own
                // cancellation apart from a client walking away, and would otherwise revoke access.
                await _membershipRepository.SetAutoRenewAsync(membership, false);

                if (string.IsNullOrWhiteSpace(membership.MpPreapprovalId)) continue;

                try
                {
                    bool stopped = await _billingService.StopAutoRenewalAsync(membership.MpPreapprovalId);
                    if (!stopped)
                    {
                        _logger.LogError(
                            "Plan {PlanId} discontinued but auto-renewal could not be stopped for membership {MembershipId} (preapproval {PreapprovalId}) — it may still be charged",
                            plan.MembershipPlanId, membership.MembershipId, membership.MpPreapprovalId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Plan {PlanId} discontinued but stopping auto-renewal threw for membership {MembershipId} (preapproval {PreapprovalId})",
                        plan.MembershipPlanId, membership.MembershipId, membership.MpPreapprovalId);
                }
            }

            await NotifySubscribersOfDiscontinuationAsync(plan, subscribers);
        }

        /// <summary>
        /// Tells affected clients when their access ends and that nothing further will be charged.
        /// One SMTP connection, and never allowed to fail the discontinue that already happened.
        /// </summary>
        private async Task NotifySubscribersOfDiscontinuationAsync(
            MembershipPlan plan,
            IEnumerable<Membership> subscribers)
        {
            var messages = new List<EmailMessage>();

            foreach (var membership in subscribers)
            {
                // Still waiting on the MP webhook: ExpirationDate is MinValue, so promise no date.
                bool hasExpiry = membership.ExpirationDate != DateTime.MinValue;

                string accessLine = hasExpiry
                    ? $"Tu membresía sigue activa hasta el <strong>{GymTime.ToGymTime(membership.ExpirationDate):dd/MM/yyyy}</strong> y no se renovará después de esa fecha."
                    : "Tu membresía sigue activa durante el período que ya abonaste y no se renovará al finalizar.";

                messages.Add(new EmailMessage
                {
                    ToEmail = membership.User.Email,
                    Subject = $"Tu plan {plan.Type} deja de estar disponible",
                    HtmlBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                        <h2 style='color: #2b2b2b;'>Tu plan deja de estar disponible</h2>
                        <p>Hola <strong>{membership.User.Name}</strong>,</p>
                        <p>El plan <strong>{plan.Type}</strong> dejó de ofrecerse en el gimnasio.</p>
                        <p>{accessLine}</p>
                        <p>No se realizarán más cobros automáticos por este plan. Cuando quieras seguir entrenando con nosotros, podés elegir uno de los planes disponibles desde tu cuenta.</p>
                        <p style='color: #666; font-size: 13px;'>Si tenés alguna duda, podés contactarnos respondiendo a este correo.</p>
                    </div>"
                });
            }

            try
            {
                var sent = await _emailService.SendBulkEmailAsync(messages);
                _logger.LogInformation(
                    "Plan {PlanId} discontinued: notified {Sent}/{Total} affected clients",
                    plan.MembershipPlanId, sent, messages.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Plan {PlanId} discontinued but the client notification batch failed",
                    plan.MembershipPlanId);
            }
        }
    }
}
