using GymManagement.Application.Common;
using GymManagement.Application.Interfaces;
using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Domain.Entities;
using GymManagement.Domain.Enums;
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
        private readonly IClassNotificationService _notifications;

        public GymClassService(
            IGymClassRepository gymClassRepository,
            ITrainerRepository trainerRepository,
            IInscriptionRepository inscriptionRepository,
            IClientRepository clientRepository,
            IGymClassScheduleRepository scheduleRepository,
            IMembershipRepository membershipRepository,
            IClassNotificationService notifications)
        {
            _gymClassRepository = gymClassRepository;
            _trainerRepository = trainerRepository;
            _inscriptionRepository = inscriptionRepository;
            _clientRepository = clientRepository;
            _scheduleRepository = scheduleRepository;
            _membershipRepository = membershipRepository;
            _notifications = notifications;
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
                ?? throw new NotFoundException("Class not found.");

            return BuildClassDetailResponse(gymClass, includeClientNames: true);
        }

        /// <summary>
        /// Detail lookup for the client-facing class page. Unlike <see cref="GetAdminClassById"/>:
        /// - classes that have already happened are treated as not found — clients shouldn't be able
        ///   to open the detail page (or book) a class whose time has passed.
        /// - other clients' names/emails are never included. The caller only needs a total count
        ///   (to show spots left) and whether *they themselves* are inscribed.
        /// </summary>
        public GymClassDetailResponse GetPublicClassById(Guid classId, Guid requestingUserId)
        {
            var gymClass = _gymClassRepository.GetById(classId)
                ?? throw new NotFoundException("Class not found.");

            if (gymClass.Schedule < GymTime.Now)
                throw new NotFoundException("Class not found.");

            var response = BuildClassDetailResponse(gymClass, includeClientNames: false);
            response.IsCurrentUserInscribed = _inscriptionRepository.IsUserRepeated(requestingUserId, classId);
            return response;
        }

        private GymClassDetailResponse BuildClassDetailResponse(GymClass gymClass, bool includeClientNames)
        {
            var inscriptions = _inscriptionRepository.GetByClassId(gymClass.GymClassId);

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
                InscribedClients = includeClientNames
                    ? inscriptions.Where(i => i.Client != null).Select(ToClientSummary).ToList()
                    : [],
                InscriptionCount = inscriptions.Count
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
                IsActive = s.IsActive,
                Trainer = s.Trainer == null ? null : new TrainerSummaryResponse
                {
                    TrainerId = s.Trainer.UserId,
                    Name = s.Trainer.Name,
                    Specialization = s.Trainer is Trainer t ? t.Specialization : null
                }
            }).ToList();

            var specialClasses = _gymClassRepository.GetAll()
                .Where(gc => gc.GymClassScheduleId == null && gc.Schedule >= GymTime.Now)
                .Select(gc => new GymClassResponse
                {
                    GymClassId = gc.GymClassId,
                    ClassName = gc.ClassName,
                    ClassDescription = gc.ClassDescription,
                    MaxCapacity = gc.MaxCapacity,
                    TrainerId = gc.TrainerId,
                    Schedule = gc.Schedule,
                    GymClassScheduleId = gc.GymClassScheduleId,
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
                Schedule = gymClass.Schedule,
                GymClassScheduleId = gymClass.GymClassScheduleId
            };
        }

        public async Task ModifyClassAsync(Guid classId, ClassRequest request, Guid requestingUserId, string userRole)
        {
            var gymClass = _gymClassRepository.GetById(classId)
                ?? throw new NotFoundException("Class not found.");

            if (userRole == "Trainer")
            {
                if (gymClass.TrainerId != requestingUserId)
                    throw new ForbiddenException("You can't modify a class that hasn't been assigned to you.");

                // A Trainer cannot reassign a class to a different trainer
                if (request.TrainerId != requestingUserId)
                    throw new ForbiddenException("You can't reassign a class to another trainer.");
            }

            // If the trainer is being changed (Admin path), validate the new trainer
            if (request.TrainerId != gymClass.TrainerId)
            {
                var newTrainer = AssertIsActiveTrainer(request.TrainerId);
                gymClass.TrainerId = request.TrainerId;
                gymClass.Trainer = newTrainer;
            }

            // Captured before the mutation so the email can show what the time used to be.
            var previousSchedule = gymClass.Schedule;
            var scheduleChanged = request.Schedule != previousSchedule;

            gymClass.ClassName = request.ClassName;
            gymClass.ClassDescription = request.ClassDescription;
            gymClass.MaxCapacity = request.MaxCapacity;
            gymClass.Schedule = request.Schedule;

            _gymClassRepository.Update(gymClass);

            // Only a moved class is worth an email — renaming it or changing its capacity
            // doesn't affect whether a client can still attend.
            if (scheduleChanged)
                await _notifications.NotifyClassRescheduledAsync(gymClass, previousSchedule);
        }

        public async Task DeleteClassAsync(Guid classId)
        {
            var gymClass = _gymClassRepository.GetById(classId)
                ?? throw new NotFoundException("Class not found.");

            // Notify first: the roster has to be readable, and this never throws.
            // Only future classes are worth an email — a past class's attendees already attended (or didn't).
            if (gymClass.Schedule >= GymTime.Now)
                await _notifications.NotifyClassesCancelledAsync([gymClass]);

            _gymClassRepository.Delete(classId);
        }

        public List<GymClassResponse> GetTrainerClasses(Guid trainerId, Guid requestingUserId, string userRole)
        {
            if (userRole == "Trainer" && trainerId != requestingUserId)
            {
                throw new ForbiddenException("You can't view another trainer's classes.");
            }

            var classes = _gymClassRepository.GetByTrainerId(trainerId);
            return [.. classes.Select(gc => new GymClassResponse
            {
                GymClassId = gc.GymClassId,
                ClassName = gc.ClassName,
                ClassDescription = gc.ClassDescription,
                MaxCapacity = gc.MaxCapacity,
                TrainerId = gc.TrainerId,
                Schedule = gc.Schedule,
                GymClassScheduleId = gc.GymClassScheduleId
            })];
        }

        public async Task JoinClassAsync(Guid clientId, Guid classId, Guid requestingUserId, string userRole)
        {
            if (userRole == "Client" && clientId != requestingUserId)
                throw new ForbiddenException("You can't enroll another client.");

            var gymClass = _gymClassRepository.GetById(classId) ?? throw new NotFoundException("Class not found.");

            if (userRole == "Trainer" && gymClass.TrainerId != requestingUserId)
                throw new ForbiddenException("You can't enroll clients in a class that isn't yours.");

            int currentCount = _inscriptionRepository.CountByClassId(classId);
            if (currentCount >= gymClass.MaxCapacity) throw new ConflictException("The class is full.");

            if (_inscriptionRepository.IsUserRepeated(clientId, classId)) throw new ConflictException("The client is already enrolled.");

            var client = _clientRepository.GetById(clientId) ?? throw new NotFoundException("Client not found.");

            var activeMembership = await _membershipRepository.GetActiveByUserId(clientId);
            if (activeMembership == null || activeMembership.ExpirationDate < DateTime.UtcNow)
                throw new ForbiddenException("This client doesn't have an active membership.");

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
                throw new ForbiddenException("You can't remove another client's enrollment.");

            var gymClass = _gymClassRepository.GetById(classId) ?? throw new NotFoundException("Class not found.");

            if (userRole == "Trainer" && gymClass.TrainerId != requestingUserId)
                throw new ForbiddenException("You can't remove enrollments from a class that isn't yours.");

            if (!_inscriptionRepository.IsUserRepeated(clientId, classId))
                throw new ConflictException("This client isn't enrolled in this class.");

            _inscriptionRepository.Remove(clientId, classId);
        }

        public List<ClientSummaryResponse> GetClientsByClass(Guid classId, Guid requestingUserId, string userRole)
        {
            var gymClass = _gymClassRepository.GetById(classId) ?? throw new NotFoundException("Class not found.");

            if (userRole == "Trainer" && gymClass.TrainerId != requestingUserId)
                throw new ForbiddenException("You can't view the clients of a class that isn't yours.");

            var inscriptions = _inscriptionRepository.GetByClassId(classId);
            return [.. inscriptions.Where(i => i.Client != null).Select(ToClientSummary)];
        }

        /// <summary>
        /// Records attendance for a whole class in one go. Only the trainer who owns the class,
        /// or an Admin, may do this, and only once the class has actually started.
        /// Returns the refreshed roster so the caller doesn't need a second request.
        /// </summary>
        public List<ClientSummaryResponse> RecordAttendance(
            Guid classId, AttendanceRequest request, Guid requestingUserId, string userRole)
        {
            var gymClass = _gymClassRepository.GetById(classId)
                ?? throw new NotFoundException("Class not found.");

            if (userRole == "Trainer" && gymClass.TrainerId != requestingUserId)
                throw new ForbiddenException("You can't record attendance for a class that isn't yours.");

            // Marking attendance before the class has started would let a trainer fill in a
            // register for sessions that haven't happened yet.
            if (gymClass.Schedule > GymTime.Now)
                throw new ConflictException("This class hasn't started yet, so attendance can't be recorded.");

            var duplicateClientIds = request.Entries
                .GroupBy(e => e.ClientId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateClientIds.Count > 0)
                throw new ValidationException("The same client appears more than once in the attendance list.");

            // C# enums accept any underlying int, so an out-of-range status would otherwise be
            // stored verbatim and read back as a value nothing knows how to render.
            if (request.Entries.Any(e => !Enum.IsDefined(e.Status)))
                throw new ValidationException("The attendance list contains an unrecognised status value.");

            var inscriptions = _inscriptionRepository.GetByClassId(classId);
            var inscriptionByClientId = inscriptions
                .Where(i => i.ClientId != null)
                .ToDictionary(i => i.ClientId!.Value);

            // Reject unknown clients outright rather than skipping them: silently ignoring an
            // entry would hide a frontend bug behind an apparently successful save.
            var notEnrolled = request.Entries
                .Where(e => !inscriptionByClientId.ContainsKey(e.ClientId))
                .ToList();

            if (notEnrolled.Count > 0)
                throw new ValidationException("The attendance list contains clients who aren't enrolled in this class.");

            var touched = new List<Inscription>();
            foreach (var entry in request.Entries)
            {
                var inscription = inscriptionByClientId[entry.ClientId];
                if (inscription.AttendanceStatus == entry.Status) continue;

                inscription.AttendanceStatus = entry.Status;
                // Clearing a mark clears its timestamp too, so the two never disagree.
                inscription.AttendanceRecordedAt = entry.Status == AttendanceStatus.NotRecorded
                    ? null
                    : DateTime.UtcNow;
                touched.Add(inscription);
            }

            if (touched.Count > 0)
                _inscriptionRepository.UpdateRange(touched);

            return [.. inscriptions.Where(i => i.Client != null).Select(ToClientSummary)];
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        /// <summary>
        /// Maps an inscription whose Client is loaded. Callers must filter out inscriptions with a
        /// null Client first — those are enrolments left behind by a role change (see
        /// <c>NullifyClientId</c>), which exist only to preserve the attendance record.
        /// </summary>
        private static ClientSummaryResponse ToClientSummary(Inscription inscription) => new()
        {
            ClientId = inscription.Client!.UserId,
            Name = inscription.Client.Name,
            Email = inscription.Client.Email,
            AttendanceStatus = inscription.AttendanceStatus,
            AttendanceRecordedAt = inscription.AttendanceRecordedAt
        };

        /// <summary>
        /// Asserts that <paramref name="trainerId"/> belongs to an active, non-deleted Trainer.
        /// Throws <see cref="NotFoundException"/> otherwise.
        /// </summary>
        private Trainer AssertIsActiveTrainer(Guid trainerId)
        {
            var trainer = _trainerRepository.GetById(trainerId)
                ?? throw new NotFoundException("Trainer not found, or the user doesn't have the Trainer role.");

            if (trainer.IsUserDeleted)
                throw new NotFoundException("This trainer has been deactivated.");

            return trainer;
        }
    }
}
