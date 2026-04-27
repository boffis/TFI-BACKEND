using Domain.Entities;
using Domain.Interfaces;

namespace Infraestructure.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        public AdminRepository() { }
       
        public List <User> GetAllUsers()
        {
            return new List<User>();
        }
    }
}
