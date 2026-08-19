using GymManagement.Application.Exceptions;
using GymManagement.Application.Interfaces;
using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace GymManagement.Application.Services
{
    public class MembershipService
    {
        private readonly IMembershipRepository _membershipRepository;
        private readonly IMembershipPlanRepository _membershipPlanRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IServiceProvider _serviceProvider;

        public MembershipService(
            IMembershipRepository membershipRepository,
            IMembershipPlanRepository membershipPlanRepository,
            IPaymentRepository paymentRepository,
            IServiceProvider serviceProvider)
        {
            _membershipRepository = membershipRepository;
            _membershipPlanRepository = membershipPlanRepository;
            _paymentRepository = paymentRepository;
            _serviceProvider = serviceProvider;
        }

        public async Task<List<Membership>> GetAllMemberships()
            => await _membershipRepository.GetAllMemberships();

        public async Task<Membership?> GetMembershipById(Guid membershipId)
            => await _membershipRepository.GetMembershipById(membershipId);

        public async Task<List<Membership>> GetMembershipsByUserId(Guid userId)
            => await _membershipRepository.GetByUserId(userId);

        public async Task<MembershipResponse> AddMembership(MembershipRequest request)
        {
            var plan = await _membershipPlanRepository.GetByIdAsync(request.MembershipPlanId)
                ?? throw new NotFoundException("Membership plan not found.");

            if (plan.IsDeleted)
                throw new ConflictException("This membership plan is no longer available.");

            await EnsureNoConflictingMembershipAsync(request.UserId);

            var membership = new Membership
            {
                MembershipId = Guid.NewGuid(),
                UserId = request.UserId,
                User = null!, // EF Core resolves navigation via FK
                MembershipPlanId = request.MembershipPlanId,
                MembershipPlan = plan,
                ExpirationDate = DateTime.MinValue,
                IsCancelled = false
            };

            await _membershipRepository.AddMembership(membership);

            return new MembershipResponse
            {
                MembershipId = membership.MembershipId,
                UserId = membership.UserId,
                MembershipPlan = new MembershipPlanResponse
                {
                    MembershipPlanId = plan.MembershipPlanId,
                    Type = plan.Type,
                    Price = plan.Price,
                    DurationInDays = plan.DurationInDays
                },
                ExpirationDate = membership.ExpirationDate,
                IsCancelled = membership.IsCancelled
            };
        }

        /// <summary>
        /// Admin path for cash paid in person: activates immediately instead of waiting on the MP
        /// webhook, and records a matching Payment row for the client's history.
        /// </summary>
        public async Task<MembershipResponse> GrantCashMembershipAsync(MembershipRequest request)
        {
            var plan = await _membershipPlanRepository.GetByIdAsync(request.MembershipPlanId)
                ?? throw new NotFoundException("Membership plan not found.");

            if (plan.IsDeleted)
                throw new ConflictException("This membership plan is no longer available.");

            await EnsureNoConflictingMembershipAsync(request.UserId);

            var membership = new Membership
            {
                MembershipId = Guid.NewGuid(),
                UserId = request.UserId,
                User = null!, // EF Core resolves navigation via FK
                MembershipPlanId = request.MembershipPlanId,
                MembershipPlan = plan,
                ExpirationDate = DateTime.UtcNow.AddDays(plan.DurationInDays),
                IsCancelled = false
            };

            await _membershipRepository.AddMembership(membership);

            var payment = new Payment
            {
                PaymentId = Guid.NewGuid(),
                UserId = request.UserId,
                User = null!, // EF Core resolves navigation via FK
                MembershipId = membership.MembershipId,
                Membership = membership,
                Price = plan.Price,
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = "cash",
                PaymentState = "approved"
            };

            _paymentRepository.AddPayment(payment);

            return new MembershipResponse
            {
                MembershipId = membership.MembershipId,
                UserId = membership.UserId,
                MembershipPlan = new MembershipPlanResponse
                {
                    MembershipPlanId = plan.MembershipPlanId,
                    Type = plan.Type,
                    Price = plan.Price,
                    DurationInDays = plan.DurationInDays
                },
                ExpirationDate = membership.ExpirationDate,
                IsCancelled = membership.IsCancelled
            };
        }

        public async Task<bool> ChangeMembershipAsync(Guid membershipId, NewMembershipRequest request)
        {
            var existingMembership = await _membershipRepository.GetMembershipById(membershipId);
            if (existingMembership == null) return false;

            // Discontinued plans are refused here, but not in ActivateMembershipAsync — that one
            // finishes memberships already bought, which must still work after a plan is retired.
            var plan = await _membershipPlanRepository.GetByIdAsync(request.MembershipPlanId);
            if (plan == null || plan.IsDeleted) return false;

            existingMembership.MembershipPlanId = request.MembershipPlanId;
            existingMembership.MembershipPlan = plan;
            existingMembership.ExpirationDate = DateTime.UtcNow.AddDays(plan.DurationInDays);

            await _membershipRepository.ChangeMembership(existingMembership);
            return true;
        }

        public async Task<bool> ActivateMembershipAsync(Guid membershipId)
        {
            var membership = await _membershipRepository.GetMembershipById(membershipId);
            if (membership == null)
                return false;

            var plan = await _membershipPlanRepository.GetByIdAsync(membership.MembershipPlanId);
            if (plan == null)
                return false;

            membership.ExpirationDate = DateTime.UtcNow.AddDays(plan.DurationInDays);

            await _membershipRepository.ChangeMembership(membership);
            return true;
        }

        public async Task<bool> CancelMembershipAsync(Guid membershipId)
        {
            var membership = await _membershipRepository.GetMembershipById(membershipId);
            if (membership == null)
                return false;
            membership.IsCancelled = true;
            await _membershipRepository.ChangeMembership(membership);
            return true;
        }

        /// <summary>
        /// One non-cancelled membership per client. An active one — or an MP subscription still
        /// awaiting webhook activation — blocks a new one; an expired one is retired here instead.
        /// Retiring goes through the billing service, not a bare IsCancelled, because an expired
        /// membership's preapproval is usually still live and would keep billing. Throws
        /// BillingUnavailableException and persists nothing if MP can't confirm the cancellation.
        /// </summary>
        /// <param name="selfService">True phrases the conflict for the client, false for admins.</param>
        public async Task EnsureNoConflictingMembershipAsync(Guid userId, bool selfService = false)
        {
            var existingMembership = await _membershipRepository.GetActiveByUserId(userId);
            if (existingMembership == null) return;

            var isPendingActivation = existingMembership.ExpirationDate == DateTime.MinValue;
            var isExpired = !isPendingActivation && existingMembership.ExpirationDate <= DateTime.UtcNow;

            if (isExpired)
            {
                // Resolved here, not injected: MercadoPagoService depends on this class, so
                // constructor injection would be a cycle the DI container refuses to build.
                var billingService = _serviceProvider.GetRequiredService<IMembershipBillingService>();
                await billingService.RetireSupersededMembershipAsync(existingMembership);
                return;
            }

            if (selfService)
            {
                throw new ConflictException(isPendingActivation
                    ? "You already have a membership awaiting activation. Cancel it from your account before subscribing to a new plan."
                    : "You already have an active membership. Cancel it from your account before subscribing to a new plan.");
            }

            throw new ConflictException(isPendingActivation
                ? "This user already has a membership awaiting activation. Activate or cancel it before creating a new one."
                : "This user already has an active membership. Change the plan on the existing membership, or cancel it before creating a new one.");
        }
    }
}
