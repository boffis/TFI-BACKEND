using GymManagement.Application.Interfaces;
using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Domain.Entities;

namespace GymManagement.Application.Services
{
    public class TrainerService
    {
        private readonly ITrainerRepository _trainerRepository;
        private readonly IGymClassRepository _gymClassRepository;

        public TrainerService(ITrainerRepository trainerRepository, IGymClassRepository gymClassRepository)
        {
            _trainerRepository = trainerRepository;
            _gymClassRepository = gymClassRepository;
        }

        public List<Trainer> GetAllTrainers()
        {
            return _trainerRepository.GetAll();
        }

        public Trainer? GetTrainerById(Guid UserId)
        {
            return _trainerRepository.GetById(UserId);
        }

        public TrainerResponse UpdateTrainer(Guid trainerId, TrainerRequest request)
        {
            var trainer = _trainerRepository.GetById(trainerId) ?? 
                throw new InvalidOperationException("Trainer no encontrado");
            trainer.Name = request.Name;
            trainer.Email = request.Email;
            trainer.Password = request.Password;
            trainer.Specialization = request.Specialization;
            _trainerRepository.Update(trainer);
            return new TrainerResponse
            {
                UserId = trainer.UserId,
                Name = trainer.Name,
                Email = trainer.Email,
                Password = trainer.Password,
                Specialization = trainer.Specialization
            };
        }

        public GymClassResponse CreateClass(Guid trainerId, ClassRequest request)
        {
            var trainer = _trainerRepository.GetById(trainerId) ?? 
                throw new InvalidOperationException("Trainer no encontrado");
            var gymClass = new GymClass
            {
                GymClassId = Guid.NewGuid(),
                ClassName = request.ClassName,
                ClassDescription = request.ClassDescription,
                MaxCapacity = request.MaxCapacity,
                Schedule = request.Schedule,
                TrainerId = trainerId,
                Trainer = trainer
            };

            _gymClassRepository.Add(gymClass);

            return new GymClassResponse
            {
                GymClassId = gymClass.GymClassId,
                ClassName = gymClass.ClassName,
                ClassDescription = gymClass.ClassDescription,
                MaxCapacity = gymClass.MaxCapacity,
                TrainerId = gymClass.TrainerId,
                Schedule = gymClass.Schedule
            };
        }

        public bool ModifyTrainerClasses(Guid trainerId, Guid classId, ClassRequest request)
        {
            var gymClass = _gymClassRepository.GetById(classId);
            if (gymClass == null || gymClass.TrainerId != trainerId)
            {
                return false;
            }

            gymClass.ClassName = request.ClassName;
            gymClass.ClassDescription = request.ClassDescription;
            gymClass.MaxCapacity = request.MaxCapacity;
            gymClass.Schedule = request.Schedule;

            _gymClassRepository.Update(gymClass);
            return true;
        }

        public bool DeleteTrainerClasses(Guid trainerId, Guid classId)
        {
            var gymClass = _gymClassRepository.GetById(classId);
            if (gymClass == null || gymClass.TrainerId != trainerId)
            {
                return false;
            }

            _gymClassRepository.Delete(gymClass);
            return true;
        }

        public List<GymClassResponse> GetTrainerClasses(Guid trainerId)
        {
            var classes = _gymClassRepository.GetByTrainerId(trainerId);
            return [.. classes.Select(gc => new GymClassResponse
            {
                GymClassId = gc.GymClassId,
                ClassName = gc.ClassName,
                ClassDescription = gc.ClassDescription,
                MaxCapacity = gc.MaxCapacity,
                TrainerId = gc.TrainerId,
                Schedule = gc.Schedule
            })];
        }
    }
}