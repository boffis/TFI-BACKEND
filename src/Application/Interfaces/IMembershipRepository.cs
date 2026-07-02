using GymManagement.Domain.Entities;

namespace GymManagement.Application.Interfaces
{
    public interface IMembershipRepository
    {
        Task<List<Membership>> GetAllMemberships();

        Task<Membership?> GetMembershipById(Guid membershipId);

        Task<Membership> AddMembership(Membership membership);

        Task ChangeMembership(Membership membership);

        Task CancelMembership(Guid membershipId);
    }
}