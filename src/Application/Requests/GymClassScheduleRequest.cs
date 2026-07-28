using System;

namespace GymManagement.Application.Requests
{
    public class GymClassScheduleRequest
    {
        public required string ClassName { get; set; }

        public string? ClassDescription { get; set; }

        public required int MaxCapacity { get; set; }

        public DayOfWeek DayOfWeek { get; set; }

        public TimeSpan TimeOfDay { get; set; }

        public bool IsWeekly { get; set; } = true;
    }
}
