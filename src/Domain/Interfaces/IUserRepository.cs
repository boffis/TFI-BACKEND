using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IUserRepository
    {
        List<User> GetUsers();
    }
}
