using System;
using System.Collections.Generic;
using System.Linq;
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

        public GymClassScheduleService(
            IGymClassScheduleRepository scheduleRepository,
            IGymClassRepository gymClassRepository,
            ITrainerRepository trainerRepository)
        {
            _scheduleRepository = scheduleRepository;
            _gymClassRepository = gymClassRepository;
            _trainerRepository = trainerRepository;
        }

        public List<GymClassScheduleResponse> GetAllSchedules()
        {
            var schedules = _scheduleRepository.GetAll();
            return [.. schedules.Select(MapToResponse)];
        }

        public GymClassScheduleResponse? GetScheduleById(Guid id, Guid requestingUserId, string userRole)
        {
            var schedule = _scheduleRepository.GetById(id) ?? throw new NotFoundException("Schedule no encontrado");

            if (userRole == "Trainer" && schedule.TrainerId != requestingUserId)
            {
                throw new ForbiddenException("No puedes ver una clase que no te pertenece.");
            }

            return MapToResponse(schedule);
        }

        public GymClassScheduleDetailResponse GetAdminScheduleById(Guid id)
        {
            var schedule = _scheduleRepository.GetById(id) ?? throw new NotFoundException("Schedule no encontrado");
            var gymClasses = _gymClassRepository.GetAll().Where(gc => gc.GymClassScheduleId == id).ToList();

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
                    // For schedule details, we might not have inscriptions eagerly loaded. 
                    // Let's pass an empty list for now, or we'd need to inject inscription repo here.
                    // But the user requested "the same dto".
                    InscribedClients = []
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
            var schedule = _scheduleRepository.GetById(scheduleId) ?? throw new NotFoundException("Schedule no encontrado");

            if (userRole == "Trainer")
            {
                if (schedule.TrainerId != requestingUserId)
                    throw new ForbiddenException("No puedes modificar un horario que no te pertenece.");

                // A Trainer cannot reassign the schedule to a different trainer
                if (request.TrainerId != requestingUserId)
                    throw new ForbiddenException("No puedes reasignar un horario a otro entrenador.");
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
                    .Where(gc => gc.GymClassScheduleId == scheduleId && gc.Schedule >= DateTime.UtcNow);

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

        public void DeleteSchedule(Guid scheduleId, bool deleteUpcomingClasses)
        {
            var schedule = _scheduleRepository.GetById(scheduleId) ?? throw new NotFoundException("Schedule no encontrado");

            _scheduleRepository.Delete(scheduleId);

            if (deleteUpcomingClasses)
            {
                var upcomingClasses = _gymClassRepository.GetAll()
                    .Where(gc => gc.GymClassScheduleId == scheduleId && gc.Schedule >= DateTime.UtcNow);

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
            var startDate = DateTime.UtcNow.Date;
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
                ?? throw new NotFoundException("Entrenador no encontrado o el usuario no tiene rol de Trainer.");

            if (trainer.IsUserDeleted)
                throw new NotFoundException("El entrenador está dado de baja.");

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
