using GymManagement.Application.Interfaces;
using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Domain.Entities;
using GymManagement.Application.Exceptions;
using System.Linq;

namespace GymManagement.Application.Services
{
    public class GymClassService : IGymClassService
    {
        private readonly IGymClassRepository _gymClassRepository;
        private readonly ITrainerRepository _trainerRepository;
        private readonly IInscriptionRepository _inscriptionRepository;
        private readonly IClientRepository _clientRepository;
        private readonly IGymClassScheduleRepository _scheduleRepository;
        private readonly IMembershipRepository _membershipRepository;

        public GymClassService(
            IGymClassRepository gymClassRepository,
            ITrainerRepository trainerRepository,
            IInscriptionRepository inscriptionRepository,
            IClientRepository clientRepository,
            IGymClassScheduleRepository scheduleRepository,
            IMembershipRepository membershipRepository)
        {
            _gymClassRepository = gymClassRepository;
            _trainerRepository = trainerRepository;
            _inscriptionRepository = inscriptionRepository;
            _clientRepository = clientRepository;
            _scheduleRepository = scheduleRepository;
            _membershipRepository = membershipRepository;
        }

        public List<GymClassdto> GetAllClasses()
        {
            var classes = _gymClassRepository.GetAll();
            return classes.Select(c => new GymClassdto
            {
                GymClassId = c.GymClassId,
                ClassName = c.ClassName,
                ClassDescription = c.ClassDescription,
                MaxCapacity = c.MaxCapacity,
                TrainerId = c.TrainerId,
                TrainerName = c.Trainer?.Name ?? string.Empty,
                Schedule = c.Schedule,
                GymClassScheduleId = c.GymClassScheduleId,
                IsClassDeleted = c.IsClassDeleted,
                InscriptionAmount = _inscriptionRepository.CountByClassId(c.GymClassId)
            }).ToList();
        }

        public List<GymClass> GetDeletedClasses() => _gymClassRepository.GetDeleted();

        public GymClass? GetClassById(Guid id) => _gymClassRepository.GetById(id);

        public GymClass? GetDeletedClassById(Guid id) => _gymClassRepository.GetDeletedById(id);

        public GymClassDetailResponse GetAdminClassById(Guid classId)
        {
            var gymClass = _gymClassRepository.GetById(classId)
                ?? throw new NotFoundException("Clase no encontrada");

            var inscriptions = _inscriptionRepository.GetByClassId(classId);

            return new GymClassDetailResponse
            {
                GymClassId = gymClass.GymClassId,
                ClassName = gymClass.ClassName,
                ClassDescription = gymClass.ClassDescription,
                MaxCapacity = gymClass.MaxCapacity,
                Schedule = gymClass.Schedule,
                GymClassScheduleId = gymClass.GymClassScheduleId,
                Trainer = new TrainerSummaryResponse
                {
                    TrainerId = gymClass.TrainerId,
                    Name = gymClass.Trainer?.Name ?? string.Empty,
                    Specialization = gymClass.Trainer is Trainer t ? t.Specialization : null
                },
                InscribedClients = inscriptions.Where(i => i.Client != null).Select(i => new ClientSummaryResponse
                {
                    ClientId = i.ClientId ?? Guid.Empty,
                    Name = i.Client!.Name,
                    Email = i.Client.Email
                }).ToList()
            };
        }

        public ScheduledAndSpecialClassesResponse GetScheduledAndSpecialClasses()
        {
            var activeSchedules = _scheduleRepository.GetActiveSchedules();

            var scheduledClasses = activeSchedules.Select(s => new GymClassScheduleResponse
            {
                GymClassScheduleId = s.GymClassScheduleId,
                ClassName = s.ClassName,
                ClassDescription = s.ClassDescription,
                MaxCapacity = s.MaxCapacity,
                TrainerId = s.TrainerId,
                DayOfWeek = s.DayOfWeek,
                TimeOfDay = s.TimeOfDay,
                IsWeekly = s.IsWeekly,
                IsActive = s.IsActive,
                Trainer = s.Trainer == null ? null : new TrainerSummaryResponse
                {
                    TrainerId = s.Trainer.UserId,
                    Name = s.Trainer.Name,
                    Specialization = s.Trainer is Trainer t ? t.Specialization : null
                }
            }).ToList();

            var specialClasses = _gymClassRepository.GetAll()
                .Where(gc => gc.GymClassScheduleId == null)
                .Select(gc => new GymClassResponse
                {
                    GymClassId = gc.GymClassId,
                    ClassName = gc.ClassName,
                    ClassDescription = gc.ClassDescription,
                    MaxCapacity = gc.MaxCapacity,
                    TrainerId = gc.TrainerId,
                    Schedule = gc.Schedule,
                    Trainer = gc.Trainer == null ? null : new TrainerSummaryResponse
                    {
                        TrainerId = gc.Trainer.UserId,
                        Name = gc.Trainer.Name,
                        Specialization = gc.Trainer is Trainer t ? t.Specialization : null
                    }
                }).ToList();

            return new ScheduledAndSpecialClassesResponse
            {
                ScheduledClasses = scheduledClasses,
                SpecialClasses = specialClasses
            };
        }

        public GymClassResponse CreateClass(Guid trainerId, ClassRequest request)
        {
            var trainer = AssertIsActiveTrainer(trainerId);

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

        public void ModifyClass(Guid classId, ClassRequest request, Guid requestingUserId, string userRole)
        {
            var gymClass = _gymClassRepository.GetById(classId)
                ?? throw new NotFoundException("Clase no encontrada");

            if (userRole == "Trainer")
            {
                if (gymClass.TrainerId != requestingUserId)
                    throw new ForbiddenException("No puedes modificar una clase que no te ha sido asignada.");

                // A Trainer cannot reassign a class to a different trainer
                if (request.TrainerId != requestingUserId)
                    throw new ForbiddenException("No puedes reasignar una clase a otro entrenador.");
            }

            // If the trainer is being changed (Admin path), validate the new trainer
            if (request.TrainerId != gymClass.TrainerId)
            {
                var newTrainer = AssertIsActiveTrainer(request.TrainerId);
                gymClass.TrainerId = request.TrainerId;
                gymClass.Trainer = newTrainer;
            }

            gymClass.ClassName = request.ClassName;
            gymClass.ClassDescription = request.ClassDescription;
            gymClass.MaxCapacity = request.MaxCapacity;
            gymClass.Schedule = request.Schedule;

            _gymClassRepository.Update(gymClass);
        }

        public void DeleteClass(Guid classId)
        {
            var gymClass = _gymClassRepository.GetById(classId)
                ?? throw new NotFoundException("Clase no encontrada");

            _gymClassRepository.Delete(classId);
        }

        public List<GymClassResponse> GetTrainerClasses(Guid trainerId, Guid requestingUserId, string userRole)
        {
            if (userRole == "Trainer" && trainerId != requestingUserId)
            {
                throw new ForbiddenException("No puedes ver las clases de otro entrenador.");
            }

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

        public void JoinClass(Guid clientId, Guid classId, Guid requestingUserId, string userRole)
        {
            if (userRole == "Client" && clientId != requestingUserId)
                throw new ForbiddenException("No puedes inscribir a otro cliente.");

            var gymClass = _gymClassRepository.GetById(classId) ?? throw new NotFoundException("Clase no encontrada.");

            if (userRole == "Trainer" && gymClass.TrainerId != requestingUserId)
                throw new ForbiddenException("No puedes inscribir clientes en una clase que no te pertenece.");

            int currentCount = _inscriptionRepository.CountByClassId(classId);
            if (currentCount >= gymClass.MaxCapacity) throw new ConflictException("La clase está llena.");

            if (_inscriptionRepository.IsUserRepeated(clientId, classId)) throw new ConflictException("El cliente ya está inscripto.");

            var client = _clientRepository.GetById(clientId) ?? throw new NotFoundException("Cliente no encontrado.");

            var activeMembership = _membershipRepository.GetActiveByUserId(clientId).Result;
            if (activeMembership == null || activeMembership.ExpirationDate < DateTime.UtcNow)
                throw new ForbiddenException("El cliente no tiene una membresía activa.");

            var inscription = new Inscription
            {
                ClientId = clientId,
                GymClassId = classId,
                Client = client,
                GymClass = gymClass
            };

            _inscriptionRepository.Add(inscription);
        }

        public void LeaveClass(Guid clientId, Guid classId, Guid requestingUserId, string userRole)
        {
            if (userRole == "Client" && clientId != requestingUserId)
                throw new ForbiddenException("No puedes eliminar la inscripción de otro cliente.");

            var gymClass = _gymClassRepository.GetById(classId) ?? throw new NotFoundException("Clase no encontrada.");

            if (userRole == "Trainer" && gymClass.TrainerId != requestingUserId)
                throw new ForbiddenException("No puedes eliminar inscripciones de una clase que no te pertenece.");

            if (!_inscriptionRepository.IsUserRepeated(clientId, classId))
                throw new ConflictException("El cliente no está inscripto en esta clase.");

            _inscriptionRepository.Remove(clientId, classId);
        }

        public List<Client> GetClientsByClass(Guid classId, Guid requestingUserId, string userRole)
        {
            var gymClass = _gymClassRepository.GetById(classId) ?? throw new NotFoundException("Clase no encontrada.");

            if (userRole == "Trainer" && gymClass.TrainerId != requestingUserId)
                throw new ForbiddenException("No puedes ver los clientes de una clase que no te pertenece.");

            var inscriptions = _inscriptionRepository.GetByClassId(classId);
            return [.. inscriptions.Where(i => i.Client != null).Select(i => i.Client!)];
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        /// <summary>
        /// Asserts that <paramref name="trainerId"/> belongs to an active, non-deleted Trainer.
        /// Throws <see cref="NotFoundException"/> otherwise.
        /// </summary>
        private Trainer AssertIsActiveTrainer(Guid trainerId)
        {
            var trainer = _trainerRepository.GetById(trainerId)
                ?? throw new NotFoundException("Entrenador no encontrado o el usuario no tiene rol de Trainer.");

            if (trainer.IsUserDeleted)
                throw new NotFoundException("El entrenador está dado de baja.");

            return trainer;
        }
    }
}
