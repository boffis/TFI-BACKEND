using GymManagement.Application.Interfaces;
using GymManagement.Domain.Entities;
using GymManagement.Infrastructure.Persistence;

namespace GymManagement.Infrastructure.Repositories
{
    public class TrainerRepository : BaseRepository<Trainer>, ITrainerRepository
    {
        public TrainerRepository(ApplicationDbContext context) : base(context)
        {

        }

        public override List<Trainer> GetAll()
        {
            return [.. _context.Trainers.Where(c => !c.IsUserDeleted)];
        }

        public override Trainer GetById(Guid UserId)
        {
            var user = _context.Trainers.FirstOrDefault(c => c.UserId == UserId && !c.IsUserDeleted);
            return user ?? throw new InvalidOperationException("Usuario no encontrado");
        }

    }
}