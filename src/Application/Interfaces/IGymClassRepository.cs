using GymManagement.Domain.Entities;

namespace GymManagement.Application.Interfaces
{
    public interface IGymClassRepository : IBaseRepository<GymClass>
    {
        List<GymClass> GetByTrainerId(Guid trainerId);

        void Delete(GymClass gymClass);
    }
}
