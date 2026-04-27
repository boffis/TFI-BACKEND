using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IAdminRepository
    {
        List<User> GetAllUsers();
    }
}
