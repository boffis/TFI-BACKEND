using GymManagement.Domain.Entities;

namespace GymManagement.Application.Interfaces
{
    public interface IAdminRepository
    {
        List<User> GetAllUsers();
        User GetUserById(Guid UserId);
        User Add(User User);
    }
}
    