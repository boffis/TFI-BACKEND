using System;
using System.Collections.Generic;
using System.Linq;
using GymManagement.Application.Interfaces;
using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Domain.Entities;

namespace GymManagement.Application.Services
{
    public class GymClassScheduleService
    {
        private readonly IGymClassScheduleRepository _scheduleRepository;
        private readonly IGymClassRepository _gymClassRepository;

        public GymClassScheduleService(
            IGymClassScheduleRepository scheduleRepository,
            IGymClassRepository gymClassRepository)
        {
            _scheduleRepository = scheduleRepository;
            _gymClassRepository = gymClassRepository;
        }

        public List<GymClassScheduleResponse> GetAllSchedules()
        {
            var schedules = _scheduleRepository.GetAll();
            return [.. schedules.Select(MapToResponse)];
        }

        public GymClassScheduleResponse? GetScheduleById(Guid id)
        {
            var schedule = _scheduleRepository.GetById(id);
            return schedule == null ? null : MapToResponse(schedule);
        }

        public List<GymClassScheduleResponse> GetSchedulesByTrainerId(Guid trainerId)
        {
            var schedules = _scheduleRepository.GetByTrainerId(trainerId);
            return [.. schedules.Select(MapToResponse)];
        }

        public GymClassScheduleResponse CreateSchedule(Guid trainerId, GymClassScheduleRequest request)
        {
            var schedule = new GymClassSchedule
            {
                GymClassScheduleId = Guid.NewGuid(),
                ClassName = request.ClassName,
                ClassDescription = request.ClassDescription,
                MaxCapacity = request.MaxCapacity,
                TrainerId = trainerId,
                DayOfWeek = request.DayOfWeek,
                TimeOfDay = request.TimeOfDay,
                IsWeekly = request.IsWeekly,
                IsActive = true
            };

            _scheduleRepository.Add(schedule);
            return MapToResponse(schedule);
        }

        public bool ModifySchedule(Guid scheduleId, GymClassScheduleRequest request)
        {
            var schedule = _scheduleRepository.GetById(scheduleId);
            if (schedule == null) return false;

            schedule.ClassName = request.ClassName;
            schedule.ClassDescription = request.ClassDescription;
            schedule.MaxCapacity = request.MaxCapacity;
            schedule.DayOfWeek = request.DayOfWeek;
            schedule.TimeOfDay = request.TimeOfDay;
            schedule.IsWeekly = request.IsWeekly;

            _scheduleRepository.Update(schedule);
            return true;
        }

        public bool DeleteSchedule(Guid scheduleId)
        {
            var schedule = _scheduleRepository.GetById(scheduleId);
            if (schedule == null) return false;

            _scheduleRepository.Delete(scheduleId);
            return true;
        }

        public List<GymClassResponse> GenerateUpcomingSessions(int weeksAhead = 2)
        {
            var createdClasses = new List<GymClassResponse>();
            var activeSchedules = _scheduleRepository.GetActiveSchedules();
            var startDate = DateTime.UtcNow.Date;
            var endDate = startDate.AddDays(weeksAhead * 7);

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
