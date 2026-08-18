using GymManagement.Domain.Entities;

namespace GymManagement.Application.Interfaces
{
    public interface IMembershipRepository
    {
        Task<List<Membership>> GetAllMemberships();

        Task<Membership?> GetMembershipById(Guid membershipId);

        Task<List<Membership>> GetByUserId(Guid userId);

        Task<Membership?> GetActiveByUserId(Guid userId);

        /// <summary>
        /// Every non-cancelled membership, in a single query. Lets callers that need the
        /// membership status of many users at once avoid one round-trip per user.
        /// </summary>
        Task<List<Membership>> GetAllActive();

        Task<List<Membership>> GetByPlanId(Guid planId);

        Task<Membership> AddMembership(Membership membership);

        Task ChangeMembership(Membership membership);

        /// <summary>
        /// Persists only the renewal flag. GetByPlanId includes the User navigation, and
        /// ChangeMembership's DbSet.Update marks the whole tracked graph as modified — which would
        /// rewrite every subscriber's User row just to turn one boolean off.
        /// </summary>
        Task SetAutoRenewAsync(Membership membership, bool autoRenew);

        Task CancelMembership(Guid membershipId);
    }
}
