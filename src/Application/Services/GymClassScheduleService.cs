using System;
using System.Collections.Generic;
using System.Linq;
using GymManagement.Application.Common;
using GymManagement.Application.Interfaces;
using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Domain.Entities;
using GymManagement.Application.Exceptions;

namespace GymManagement.Application.Services
{
    public class GymClassScheduleService
    {
        private readonly IGymClassScheduleRepository _scheduleRepository;
        private readonly IGymClassRepository _gymClassRepository;
        private readonly ITrainerRepository _trainerRepository;
        private readonly IInscriptionRepository _inscriptionRepository;
        private readonly IClassNotificationService _notifications;

        public GymClassScheduleService(
            IGymClassScheduleRepository scheduleRepository,
            IGymClassRepository gymClassRepository,
            ITrainerRepository trainerRepository,
            IInscriptionRepository inscriptionRepository,
            IClassNotificationService notifications)
        {
            _scheduleRepository = scheduleRepository;
            _gymClassRepository = gymClassRepository;
            _trainerRepository = trainerRepository;
            _inscriptionRepository = inscriptionRepository;
            _notifications = notifications;
        }

        public List<GymClassScheduleResponse> GetAllSchedules()
        {
            var schedules = _scheduleRepository.GetAll();
            return [.. schedules.Select(MapToResponse)];
        }

        public GymClassScheduleResponse? GetScheduleById(Guid id, Guid requestingUserId, string userRole)
        {
            var schedule = _scheduleRepository.GetById(id) ?? throw new NotFoundException("Schedule not found.");

            if (userRole == "Trainer" && schedule.TrainerId != requestingUserId)
            {
                throw new ForbiddenException("You can't view a class that isn't yours.");
            }

            return MapToResponse(schedule);
        }

        public GymClassScheduleDetailResponse GetAdminScheduleById(Guid id)
        {
            var schedule = _scheduleRepository.GetById(id) ?? throw new NotFoundException("Schedule not found.");
            var gymClasses = _gymClassRepository.GetAll().Where(gc => gc.GymClassScheduleId == id).ToList();

            return BuildScheduleDetailResponse(schedule, gymClasses);
        }

        /// <summary>
        /// Detail lookup for the client-facing schedule page. Unlike <see cref="GetAdminScheduleById"/>,
        /// only sessions that haven't happened yet are included — clients shouldn't see past sessions
        /// listed as bookable.
        /// </summary>
        public GymClassScheduleDetailResponse GetPublicScheduleById(Guid id)
        {
            var schedule = _scheduleRepository.GetById(id) ?? throw new NotFoundException("Schedule not found.");
            var gymClasses = _gymClassRepository.GetAll()
                .Where(gc => gc.GymClassScheduleId == id && gc.Schedule >= GymTime.Now)
                .ToList();

            return BuildScheduleDetailResponse(schedule, gymClasses);
        }

        private GymClassScheduleDetailResponse BuildScheduleDetailResponse(GymClassSchedule schedule, List<GymClass> gymClasses)
        {
            var inscriptionCounts = gymClasses.ToDictionary(
                gc => gc.GymClassId,
                gc => _inscriptionRepository.CountByClassId(gc.GymClassId));

            return new GymClassScheduleDetailResponse
            {
                GymClassScheduleId = schedule.GymClassScheduleId,
                ClassName = schedule.ClassName,
                ClassDescription = schedule.ClassDescription,
                MaxCapacity = schedule.MaxCapacity,
                DayOfWeek = schedule.DayOfWeek,
                TimeOfDay = schedule.TimeOfDay,
                IsWeekly = schedule.IsWeekly,
                IsActive = schedule.IsActive,
                Trainer = new TrainerSummaryResponse
                {
                    TrainerId = schedule.TrainerId,
                    Name = schedule.Trainer?.Name ?? string.Empty,
                    Specialization = schedule.Trainer is Trainer t ? t.Specialization : null
                },
                GymClasses = gymClasses.Select(gc => new GymClassDetailResponse
                {
                    GymClassId = gc.GymClassId,
                    ClassName = gc.ClassName,
                    ClassDescription = gc.ClassDescription,
                    MaxCapacity = gc.MaxCapacity,
                    Schedule = gc.Schedule,
                    GymClassScheduleId = gc.GymClassScheduleId,
                    Trainer = new TrainerSummaryResponse
                    {
                        TrainerId = gc.TrainerId,
                        Name = gc.Trainer?.Name ?? string.Empty,
                        Specialization = gc.Trainer is Trainer tr ? tr.Specialization : null
                    },
                    InscriptionCount = inscriptionCounts[gc.GymClassId]
                }).ToList()
            };
        }

        public List<GymClassScheduleResponse> GetSchedulesByTrainerId(Guid trainerId)
        {
            var schedules = _scheduleRepository.GetByTrainerId(trainerId);
            return [.. schedules.Select(MapToResponse)];
        }

        public GymClassScheduleResponse CreateSchedule(Guid trainerId, GymClassScheduleRequest request)
        {
            var trainer = AssertIsActiveTrainer(trainerId);

            var schedule = new GymClassSchedule
            {
                GymClassScheduleId = Guid.NewGuid(),
                ClassName = request.ClassName,
                ClassDescription = request.ClassDescription,
                MaxCapacity = request.MaxCapacity,
                TrainerId = trainerId,
                Trainer = trainer,
                DayOfWeek = request.DayOfWeek,
                TimeOfDay = request.TimeOfDay,
                IsWeekly = request.IsWeekly,
                IsActive = true
            };

            _scheduleRepository.Add(schedule);
            return MapToResponse(schedule);
        }

        public void ModifySchedule(Guid scheduleId, GymClassScheduleRequest request, Guid requestingUserId, string userRole, bool updateUpcomingClasses)
        {
            var schedule = _scheduleRepository.GetById(scheduleId) ?? throw new NotFoundException("Schedule not found.");

            if (userRole == "Trainer")
            {
                if (schedule.TrainerId != requestingUserId)
                    throw new ForbiddenException("You can't modify a schedule that isn't yours.");

                // A Trainer cannot reassign the schedule to a different trainer
                if (request.TrainerId != requestingUserId)
                    throw new ForbiddenException("You can't reassign a schedule to another trainer.");
            }

            // If the trainer is being changed (Admin path), validate the new trainer
            Trainer? newTrainer = null;
            if (request.TrainerId != schedule.TrainerId)
            {
                newTrainer = AssertIsActiveTrainer(request.TrainerId);
                schedule.TrainerId = request.TrainerId;
                schedule.Trainer = newTrainer;
            }

            schedule.ClassName = request.ClassName;
            schedule.ClassDescription = request.ClassDescription;
            schedule.MaxCapacity = request.MaxCapacity;
            schedule.DayOfWeek = request.DayOfWeek;
            schedule.TimeOfDay = request.TimeOfDay;
            schedule.IsWeekly = request.IsWeekly;

            _scheduleRepository.Update(schedule);

            if (updateUpcomingClasses)
            {
                var upcomingClasses = _gymClassRepository.GetAll()
                    .Where(gc => gc.GymClassScheduleId == scheduleId && gc.Schedule >= GymTime.Now);

                foreach (var gymClass in upcomingClasses)
                {
                    gymClass.ClassName = request.ClassName;
                    gymClass.ClassDescription = request.ClassDescription;
                    gymClass.MaxCapacity = request.MaxCapacity;
                    // If DayOfWeek or TimeOfDay changed, we'd theoretically need to recalculate Schedule Date.
                    // For now, we only update metadata. Modifying the actual datetime of already generated classes might require more complex logic.
                    //!Im Not Doing That

                    // Propagate trainer change to upcoming classes
                    if (newTrainer != null)
                    {
                        gymClass.TrainerId = newTrainer.UserId;
                        gymClass.Trainer = newTrainer;
                    }

                    _gymClassRepository.Update(gymClass);
                }
            }
        }

        public async Task DeleteScheduleAsync(Guid scheduleId, bool deleteUpcomingClasses)
        {
            var schedule = _scheduleRepository.GetById(scheduleId) ?? throw new NotFoundException("Schedule not found.");

            _scheduleRepository.Delete(scheduleId);

            if (deleteUpcomingClasses)
            {
                var upcomingClasses = _gymClassRepository.GetAll()
                    .Where(gc => gc.GymClassScheduleId == scheduleId && gc.Schedule >= GymTime.Now)
                    .ToList();

                // One batch for every affected session, so a schedule with weeks of upcoming
                // classes still costs a single SMTP connection rather than one per email.
                await _notifications.NotifyClassesCancelledAsync(upcomingClasses);

                foreach (var gymClass in upcomingClasses)
                {
                    _gymClassRepository.Delete(gymClass.GymClassId);
                }
            }
        }

        public List<GymClassResponse> GenerateUpcomingSessions(int daysAhead = 2)
        {
            var createdClasses = new List<GymClassResponse>();
            var activeSchedules = _scheduleRepository.GetActiveSchedules();
            // Local date, not UTC: sessions are built as `date + schedule.TimeOfDay`, and
            // TimeOfDay is a wall-clock time. Starting from the UTC date would generate for
            // the wrong day whenever the two disagree (any evening in Argentina).
            var startDate = GymTime.Today;
            var endDate = startDate.AddDays(daysAhead);

            foreach (var schedule in activeSchedules)
            {
                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    if (date.DayOfWeek == schedule.DayOfWeek)
                    {
                        var sessionDateTime = date.Date + schedule.TimeOfDay;

                        // Check duplicate
                        if (!_gymClassRepository.Exists(schedule.GymClassScheduleId, sessionDateTime))
                        {
                            var newGymClass = new GymClass
                            {
                                GymClassId = Guid.NewGuid(),
                                GymClassScheduleId = schedule.GymClassScheduleId,
                                ClassName = schedule.ClassName,
                                ClassDescription = schedule.ClassDescription,
                                MaxCapacity = schedule.MaxCapacity,
                                TrainerId = schedule.TrainerId,
                                Trainer = schedule.Trainer!,
                                Schedule = sessionDateTime,
                                IsClassDeleted = false
                            };

                            var added = _gymClassRepository.Add(newGymClass);

                            createdClasses.Add(new GymClassResponse
                            {
                                GymClassId = added.GymClassId,
                                ClassName = added.ClassName,
                                ClassDescription = added.ClassDescription,
                                MaxCapacity = added.MaxCapacity,
                                TrainerId = added.TrainerId,
                                Schedule = added.Schedule
                            });
                        }
                    }
                }
            }

            return createdClasses;
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
                ?? throw new NotFoundException("Trainer not found, or the user doesn't have the Trainer role.");

            if (trainer.IsUserDeleted)
                throw new NotFoundException("This trainer has been deactivated.");

            return trainer;
        }

        private static GymClassScheduleResponse MapToResponse(GymClassSchedule schedule)
        {
            return new GymClassScheduleResponse
            {
                GymClassScheduleId = schedule.GymClassScheduleId,
                ClassName = schedule.ClassName,
                ClassDescription = schedule.ClassDescription,
                MaxCapacity = schedule.MaxCapacity,
                TrainerId = schedule.TrainerId,
                DayOfWeek = schedule.DayOfWeek,
                TimeOfDay = schedule.TimeOfDay,
                IsWeekly = schedule.IsWeekly,
                IsActive = schedule.IsActive
            };
        }
    }
}
