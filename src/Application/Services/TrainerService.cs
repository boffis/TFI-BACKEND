using GymManagement.Application.Interfaces;
using GymManagement.Application.Mappers;
using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Domain.Entities;

namespace GymManagement.Application.Services
{
    public class TrainerService
    {
        private readonly ITrainerRepository _trainerRepository;

        public TrainerService(ITrainerRepository trainerRepository)
        {
            _trainerRepository = trainerRepository;
        }

        public List<Trainer> GetAllTrainers()
        {
            return _trainerRepository.GetAll();
        }

        public Trainer GetTrainerById(Guid UserId)
        {
            return _trainerRepository.GetById(UserId);
        }
    }
}