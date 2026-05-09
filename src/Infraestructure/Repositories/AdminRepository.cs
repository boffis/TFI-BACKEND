using GymManagement.Application.Interfaces;
using GymManagement.Domain.Entities;
using GymManagement.Infrastructure.Persistence;
using GymManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Repositories
{
    public class AdminRepository : BaseRepository<User>, IAdminRepository
    {
        public AdminRepository(ApplicationDbContext context) : base(context) 
        {
        
        }

        public List<User> GetAllUsers()
        {
            return _context.Users
                .Where(c => !c.IsUserDeleted)
                .ToList();
        }

        public User GetUserById(Guid UserId)
        {
            var user = _context.Users
                .FirstOrDefault(c => c.UserId == UserId && !c.IsUserDeleted);
            if (user == null)
                throw new InvalidOperationException("Usuario no encontrado");

            return user;
        }

        public override User Add(User user)
        {
            return user;
        }
    }
}
