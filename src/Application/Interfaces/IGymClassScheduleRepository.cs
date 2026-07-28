using System;
using System.Collections.Generic;
using GymManagement.Domain.Entities;

namespace GymManagement.Application.Interfaces
{
    public interface IGymClassScheduleRepository
    {
        List<GymClassSchedule> GetAll();

        List<GymClassSchedule> GetActiveSchedules();

        GymClassSchedule? GetById(Guid id);

        List<GymClassSchedule> GetByTrainerId(Guid trainerId);

        GymClassSchedule Add(GymClassSchedule schedule);

        void Update(GymClassSchedule schedule);

        void Delete(Guid id);
    }
}
