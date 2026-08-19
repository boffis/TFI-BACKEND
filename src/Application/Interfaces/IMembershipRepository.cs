using GymManagement.Domain.Entities;

namespace GymManagement.Application.Interfaces
{
    public interface IMembershipRepository
    {
        Task<List<Membership>> GetAllMemberships();

        Task<Membership?> GetMembershipById(Guid membershipId);

        Task<List<Membership>> GetByUserId(Guid userId);

        Task<Membership?> GetActiveByUserId(Guid userId);

        /// <summary>Every non-cancelled membership in one query, avoiding a round-trip per user.</summary>
        Task<List<Membership>> GetAllActive();

        Task<List<Membership>> GetByPlanId(Guid planId);

        Task<Membership> AddMembership(Membership membership);

        Task ChangeMembership(Membership membership);

        /// <summary>
        /// Persists only the renewal flag: DbSet.Update would mark the whole tracked graph modified
        /// and rewrite every subscriber's User row to turn one boolean off.
        /// </summary>
        Task SetAutoRenewAsync(Membership membership, bool autoRenew);

        Task CancelMembership(Guid membershipId);
    }
}
