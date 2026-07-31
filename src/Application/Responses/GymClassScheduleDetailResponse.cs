using System;
using System.Collections.Generic;

namespace GymManagement.Application.Responses
{
    public class GymClassScheduleDetailResponse
    {
        public Guid GymClassScheduleId { get; set; }
        public required string ClassName { get; set; }
        public string? ClassDescription { get; set; }
        public required int MaxCapacity { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan TimeOfDay { get; set; }
        public bool IsWeekly { get; set; }
        public bool IsActive { get; set; }
        
        public TrainerSummaryResponse Trainer { get; set; } = null!;
        public ICollection<GymClassDetailResponse> GymClasses { get; set; } = [];
    }
}
