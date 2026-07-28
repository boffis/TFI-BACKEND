using System;
using System.Collections.Generic;

namespace GymManagement.Domain.Entities
{
    public class GymClassSchedule
    {
        public Guid GymClassScheduleId { get; set; }

        public required string ClassName { get; set; }

        public string? ClassDescription { get; set; }

        public required int MaxCapacity { get; set; }

        public Guid TrainerId { get; set; }

        public Trainer? Trainer { get; set; }

        public DayOfWeek DayOfWeek { get; set; }

        public TimeSpan TimeOfDay { get; set; }

        public bool IsWeekly { get; set; } = true;

        public bool IsActive { get; set; } = true;

        public ICollection<GymClass> GymClasses { get; set; } = new List<GymClass>();
    }
}
