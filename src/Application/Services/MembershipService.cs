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

        public MembershipService(
            IMembershipRepository membershipRepository,
            IMembershipPlanRepository membershipPlanRepository)
        {
            _membershipRepository = membershipRepository;
            _membershipPlanRepository = membershipPlanRepository;
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
                ?? throw new ArgumentException("Plan de membresía no encontrado");

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
                ExpirationDate = membership.ExpirationDate
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
