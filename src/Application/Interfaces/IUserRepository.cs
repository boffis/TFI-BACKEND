using GymManagement.Domain.Entities;

namespace GymManagement.Application.Interfaces
{
    public interface IUserRepository
    {
        User? GetUserByEmail(string email);
        void RemoveUser(User user);
        void UpdateUser(User user);
    }
}
