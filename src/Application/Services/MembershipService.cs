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

            // A client may hold at most one non-cancelled membership at a time. Switching plan or
            // renewing goes through ChangeMembership (which resets ExpirationDate), and stopping
            // goes through CancelMembership. Without this guard, repeated calls here stack up
            // parallel membership rows for the same user and it becomes arbitrary which one counts
            // as "theirs" — GetActiveByUserId just takes the first match.
            var existingMembership = await _membershipRepository.GetActiveByUserId(request.UserId);
            if (existingMembership != null)
            {
                throw new ConflictException(existingMembership.ExpirationDate > DateTime.UtcNow
                    ? "This user already has an active membership. Change the plan on the existing membership, or cancel it before creating a new one."
                    : "This user already has a membership awaiting activation. Activate or cancel it before creating a new one.");
            }

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

            var existingMembership = await _membershipRepository.GetActiveByUserId(request.UserId);
            if (existingMembership != null)
            {
                throw new ConflictException(existingMembership.ExpirationDate > DateTime.UtcNow
                    ? "This user already has an active membership. Change the plan on the existing membership, or cancel it before creating a new one."
                    : "This user already has a membership awaiting activation. Activate or cancel it before creating a new one.");
            }

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

            var plan = await _membershipPlanRepository.GetByIdAsync(request.MembershipPlanId);
            if (plan == null) return false;

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
    }
}
