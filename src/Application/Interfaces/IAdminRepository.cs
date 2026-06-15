using GymManagement.Domain.Entities;

namespace GymManagement.Application.Interfaces
{
    public interface IAdminRepository : IBaseRepository<User>
    {
        User? GetUserDeleted(Guid UserId);
    }
}