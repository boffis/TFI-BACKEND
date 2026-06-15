using GymManagement.Application.Interfaces;
using GymManagement.Application.Mappers;
using GymManagement.Application.Requests;
using GymManagement.Domain.Entities;
using GymManagement.Infrastructure.Persistence;

namespace GymManagement.Infrastructure.Repositories
{
    public class AdminRepository : BaseRepository<User>, IAdminRepository
    {
        public AdminRepository(ApplicationDbContext context) : base(context)
        {

        }

        public User? GetUserDeleted(Guid UserId)
        {
            return _context.Users.FirstOrDefault(c => c.UserId == UserId && c.IsUserDeleted == true);
        }
    }
}