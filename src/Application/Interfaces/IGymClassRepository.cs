using GymManagement.Domain.Entities;

namespace GymManagement.Application.Interfaces
{
    public interface IGymClassRepository
    {
        List<GymClass> GetAll();

        List<GymClass> GetDeleted();

        GymClass? GetById(Guid id);

        GymClass? GetDeletedById(Guid id);

        List<GymClass> GetByTrainerId(Guid trainerId);

        void Update(GymClass gymClass);

        void Delete(Guid id);

        void Recover(Guid id);

        GymClass Add(GymClass gymClass);

        bool Exists(Guid gymClassScheduleId, DateTime scheduleDateTime);
    }
}