using System;
using GymManagement.Application.Common;

namespace GymManagement.Application.Responses
{
    public class GymClassResponse
    {
        /// <summary>
        /// Whether the class has begun, in the gym's time zone. Computed here because
        /// <see cref="Schedule"/> is zone-less, so a browser comparing it to its own clock is only
        /// right when the viewer sits in the gym's zone.
        /// </summary>
        public bool HasStarted => Schedule <= GymTime.Now;

        public Guid GymClassId { get; set; }

        public string ClassName { get; set; } = string.Empty;

        public string? ClassDescription { get; set; }

        public int MaxCapacity { get; set; }

        public Guid TrainerId { get; set; }

        public DateTime Schedule { get; set; }

        /// <summary>
        /// The recurring schedule this session came from, or null for a one-off "special" class.
        /// Clients tell the two apart by the null-ness, so every mapping must set it.
        /// </summary>
        public Guid? GymClassScheduleId { get; set; }

        public TrainerSummaryResponse? Trainer { get; set; }
    }
}
