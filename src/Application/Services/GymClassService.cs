using GymManagement.Application.Interfaces;
using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Domain.Entities;

namespace GymManagement.Application.Services
{
    public class GymClassService
    {
        private readonly IGymClassRepository _gymClassRepository;
        private readonly ITrainerRepository _trainerRepository;
        private readonly IInscriptionRepository _inscriptionRepository;
        private readonly IClientRepository _clientRepository;

        public GymClassService(
            IGymClassRepository gymClassRepository,
            ITrainerRepository trainerRepository, 
            IInscriptionRepository inscriptionRepository,
            IClientRepository clientRepository)
        {
            _gymClassRepository = gymClassRepository;
            _trainerRepository = trainerRepository;
            _inscriptionRepository = inscriptionRepository;
            _clientRepository = clientRepository;
        }

        public List<GymClass> GetAllClasses() => _gymClassRepository.GetAll();
        
        public List<GymClass> GetDeletedClasses() => _gymClassRepository.GetDeleted();
      
        public GymClass? GetClassById(Guid id) => _gymClassRepository.GetById(id);
        
        public GymClass? GetDeletedClassById(Guid id) => _gymClassRepository.GetDeletedById(id);      

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

            _gymClassRepository.Delete(classId);
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

        public bool JoinClass(Guid clientId, Guid classId)
        {
            var gymClass = _gymClassRepository.GetById(classId);
            if (gymClass == null) return false;

            int currentCount = _inscriptionRepository.CountByClassId(classId);
            if (currentCount >= gymClass.MaxCapacity) return false;

            if (_inscriptionRepository.IsUserRepeated(clientId, classId)) return false;

            var client = _clientRepository.GetById(clientId);

            if (client == null) return false;

            var inscription = new Inscription
            {
                ClientId = clientId,
                GymClassId = classId,
                Client = client,
                GymClass = gymClass,
                ClassDate = DateTime.UtcNow
            };

            _inscriptionRepository.Add(inscription);
            return true;
        }

        public bool LeaveClass(Guid clientId, Guid classId)
        {
            if (!_inscriptionRepository.IsUserRepeated(clientId, classId))
                return false;

            _inscriptionRepository.Remove(clientId, classId);
            return true;
        }

        public List<Client> GetClientsByClass(Guid classId)
        {
            var inscriptions = _inscriptionRepository.GetByClassId(classId);
            return [.. inscriptions.Select(i => i.Client)];
        }
    }
}