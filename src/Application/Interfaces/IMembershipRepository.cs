using GymManagement.Domain.Entities;

namespace GymManagement.Application.Interfaces
{
    public interface IMembershipRepository
    {
        Task<List<Membership>> GetAllMemberships();

        Task<Membership?> GetMembershipById(Guid membershipId);

        Task<List<Membership>> GetByUserId(Guid userId);

        Task<Membership?> GetActiveByUserId(Guid userId);

        Task<List<Membership>> GetByPlanId(Guid planId);

        Task<Membership> AddMembership(Membership membership);

        Task ChangeMembership(Membership membership);

        Task CancelMembership(Guid membershipId);
    }
}
