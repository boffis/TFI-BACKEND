using GymManagement.Application.Interfaces;
using GymManagement.Domain.Entities;
using GymManagement.Infrastructure.Persistence;

namespace GymManagement.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public User? GetUserByEmail(string email)
        {
            Client? client = _context.Clients.FirstOrDefault(c => c.Email == email && !c.IsUserDeleted);
            if (client != null) return client;

            Trainer? trainer = _context.Trainers.FirstOrDefault(t => t.Email == email && !t.IsUserDeleted);
            if (trainer != null) return trainer;

            Admin? admin = _context.Admins.FirstOrDefault(a => a.Email == email && !a.IsUserDeleted);
            if (admin != null) return admin;

            return null;
        }

        public void RemoveUser(User user)
        {
            if (user is Client client) _context.Clients.Remove(client);
            else if (user is Trainer trainer) _context.Trainers.Remove(trainer);
            else if (user is Admin admin) _context.Admins.Remove(admin);
            
            _context.SaveChanges();
        }

        public void UpdateUser(User user)
        {
            if (user is Client client) _context.Clients.Update(client);
            else if (user is Trainer trainer) _context.Trainers.Update(trainer);
            else if (user is Admin admin) _context.Admins.Update(admin);

            _context.SaveChanges();
        }
    }
}
