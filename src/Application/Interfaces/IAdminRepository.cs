using GymManagement.Domain.Entities;

namespace GymManagement.Application.Interfaces
{
    public interface IAdminRepository
    {
        List<User> GetAll();

        User GetById(Guid UserId);

        User GetUserDeleted(Guid UserId);

        void Update(User User); 

        void Delete(Guid UserId);

        void Recover(Guid UserId);
    }
}