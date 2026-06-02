using GymManagement.Domain.Entities;

namespace GymManagement.Application.Interfaces
{
    public interface ITrainerRepository
    {
        List<Trainer> GetAll();

        Trainer GetById(Guid UserId);

    }
}