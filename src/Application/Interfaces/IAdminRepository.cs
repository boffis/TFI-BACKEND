using GymManagement.Domain.Entities;

namespace GymManagement.Application.Interfaces
{
    public interface IAdminRepository
    {
        List<User> GetAllUsers();

        User GetUserById(Guid UserId);

        User GetUserDeleted(Guid UserId);

        User Add(User User);

        void Update(User User); 

        void Delete(Guid UserId);

        void RecoverUser(Guid UserId);
    }
}
    