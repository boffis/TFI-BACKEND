using System;


namespace GymManagement.Application.Responses
{
    public class GymClassScheduleResponse
    {
        public Guid GymClassScheduleId { get; set; }

        public string ClassName { get; set; } = string.Empty;

        public string? ClassDescription { get; set; }

        public int MaxCapacity { get; set; }

        public Guid TrainerId { get; set; }

        public DayOfWeek DayOfWeek { get; set; }

        public TimeSpan TimeOfDay { get; set; }

        public bool IsWeekly { get; set; }

        public bool IsActive { get; set; }

        public TrainerSummaryResponse? Trainer { get; set; }
    }
}
