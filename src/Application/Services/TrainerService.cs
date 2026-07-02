using GymManagement.Application.Interfaces;
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

        public List<Trainer> GetAllTrainers() => _trainerRepository.GetAll();

        public Trainer? GetTrainerById(Guid id) => _trainerRepository.GetById(id);

        public List<Trainer> GetDeletedTrainers() => _trainerRepository.GetDeleteds();

        public Trainer? GetDeletedTrainerById(Guid id) => _trainerRepository.GetDeletedById(id);

        public bool UpdateTrainer(Guid id, TrainerRequest request)
        {
            var trainer = _trainerRepository.GetById(id);
            if (trainer == null) return false;

            trainer.Name = request.Name;
            trainer.Email = request.Email;
            trainer.Password = request.Password;
            trainer.Specialization = request.Specialization;

            _trainerRepository.Update(trainer);
            return true;
        }

        public bool DeleteTrainer(Guid id)
        {
            var trainer = _trainerRepository.GetById(id);
            if (trainer == null) return false;

            _trainerRepository.Delete(id);
            return true;
        }

        public bool RecoverTrainer(Guid id)
        {
            var trainer = _trainerRepository.GetDeletedById(id);
            if (trainer == null) return false;

            _trainerRepository.Recover(id);
            return true;
        }
    }
}