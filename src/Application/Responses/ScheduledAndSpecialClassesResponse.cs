using System.Collections.Generic;

namespace GymManagement.Application.Responses
{
    public class ScheduledAndSpecialClassesResponse
    {
        /// <summary>
        /// All active GymClassSchedule entries.
        /// </summary>
        public List<GymClassScheduleResponse> ScheduledClasses { get; set; } = [];

        /// <summary>
        /// All active GymClass entries that are NOT linked to any GymClassSchedule (i.e. one-off / special classes).
        /// </summary>
        public List<GymClassResponse> SpecialClasses { get; set; } = [];
    }
}
