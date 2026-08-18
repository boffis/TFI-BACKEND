using GymManagement.Application.Exceptions;
using GymManagement.Application.Interfaces;
using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Domain.Entities;

namespace GymManagement.Application.Services
{
    public class MembershipService
    {
        private readonly IMembershipRepository _membershipRepository;
        private readonly IMembershipPlanRepository _membershipPlanRepository;
        private readonly IPaymentRepository _paymentRepository;

        public MembershipService(
            IMembershipRepository membershipRepository,
            IMembershipPlanRepository membershipPlanRepository,
            IPaymentRepository paymentRepository)
        {
            _membershipRepository = membershipRepository;
            _membershipPlanRepository = membershipPlanRepository;
            _paymentRepository = paymentRepository;
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
        /// Admin-only path for a client who paid in cash, outside Mercado Pago. Unlike
        /// AddMembership (which starts the membership pending until the MP webhook activates it),
        /// this activates it immediately since the cash payment was already received in person, and
        /// records a matching Payment row so it shows up in the client's payment history.
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

            // A discontinued plan is refused here as well: moving a client onto one would hand them
            // a membership that renews on a plan the gym no longer sells. ActivateMembershipAsync
            // deliberately does not check this — it finishes activating memberships that were
            // already bought, which must still work after the plan is discontinued.
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
        /// A client may hold at most one non-cancelled membership at a time. An existing membership
        /// that is still active, or a Mercado Pago subscription still awaiting webhook activation
        /// (ExpirationDate left at DateTime.MinValue), blocks creating a new one until it's changed
        /// or cancelled. An expired membership no longer counts as "theirs" though — it's cancelled
        /// here automatically so it can't be mistaken for the active one, and the caller is free to
        /// create the replacement.
        /// </summary>
        /// <param name="selfService">
        /// True when the client is buying for themselves, so the conflict is phrased for them and
        /// points at the cancel button on their account page. False for the admin paths, which speak
        /// about the client in the third person and can also just switch the existing plan.
        /// </param>
        public async Task EnsureNoConflictingMembershipAsync(Guid userId, bool selfService = false)
        {
            var existingMembership = await _membershipRepository.GetActiveByUserId(userId);
            if (existingMembership == null) return;

            var isPendingActivation = existingMembership.ExpirationDate == DateTime.MinValue;
            var isExpired = !isPendingActivation && existingMembership.ExpirationDate <= DateTime.UtcNow;

            if (isExpired)
            {
                existingMembership.IsCancelled = true;
                await _membershipRepository.ChangeMembership(existingMembership);
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
