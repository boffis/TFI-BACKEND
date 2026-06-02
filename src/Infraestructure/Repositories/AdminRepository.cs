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

        public override List<User> GetAll()
        {
            return [.. _context.Users.Where(c => !c.IsUserDeleted)];
        }

        public override User GetById(Guid UserId)
        {
            var user = _context.Users
                .FirstOrDefault(c => c.UserId == UserId && !c.IsUserDeleted);
            return user ?? throw new InvalidOperationException("Usuario no encontrado");
        }

        public User GetUserDeleted(Guid UserId)
        {
            var user = _context.Users
                .FirstOrDefault(c => c.UserId == UserId);
            return user ?? throw new InvalidOperationException("Usuario no encontrado");
        }

        public override void Update(User user)
        {
            _context.Users.Update(user);
            _context.SaveChanges();
        }

        public override void Delete(Guid UserId)
        {
            var user = GetById(UserId) ??
                throw new InvalidOperationException("Usuario no encontrado");
            user.IsUserDeleted = true;
            _dbSet.Update(user);
            _context.SaveChanges();
        }

        public override void Recover(Guid UserId)
        {
            var user = GetUserDeleted(UserId) ??
                throw new InvalidOperationException("Usuario no encontrado");
            user.IsUserDeleted = false;
            _dbSet.Update(user);
            _context.SaveChanges();
        }
    }
}