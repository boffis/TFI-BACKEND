using System.Collections.Generic;

namespace GymManagement.Application.Responses
{
    public class ScheduledAndSpecialClassesResponse
    {
        /// <summary>All active GymClassSchedule entries.</summary>
        public List<GymClassScheduleResponse> ScheduledClasses { get; set; } = [];

        /// <summary>Active one-off classes — those not linked to any GymClassSchedule.</summary>
        public List<GymClassResponse> SpecialClasses { get; set; } = [];
    }
}
