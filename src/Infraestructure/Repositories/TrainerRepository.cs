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

        public List<Trainer> GetAllTrainers() => GetAll();

        public Trainer? GetTrainerById(Guid id) => GetById(id);

        public List<Trainer> GetDeletedTrainers() => GetDeleteds();

        public Trainer? GetDeletedTrainerById(Guid id) => GetDeletedById(id);

        public Trainer AddTrainer(Trainer trainer) => Add(trainer);

        public void UpdateTrainer(Trainer trainer) => Update(trainer);

        public void DeleteTrainer(Guid id) => Delete(id);

        public void RecoverTrainer(Guid id) => Recover(id);
    }
}
