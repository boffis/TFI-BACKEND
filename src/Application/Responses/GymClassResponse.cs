using System;
using GymManagement.Application.Common;

namespace GymManagement.Application.Responses
{
    public class GymClassResponse
    {
        /// <summary>
        /// Whether the class has already begun, in the gym's own time zone.
        /// <para>
        /// Computed here rather than by the client: <see cref="Schedule"/> is a wall-clock value with
        /// no time zone, so a browser comparing it against its own clock only gets the right answer
        /// when the viewer happens to be in the same zone as the gym. Deriving it on the DTO also
        /// means every place that builds this response gets it right without having to remember.
        /// </para>
        /// </summary>
        public bool HasStarted => Schedule <= GymTime.Now;

        public Guid GymClassId { get; set; }

        public string ClassName { get; set; } = string.Empty;

        public string? ClassDescription { get; set; }

        public int MaxCapacity { get; set; }

        public Guid TrainerId { get; set; }

        public DateTime Schedule { get; set; }

        public TrainerSummaryResponse? Trainer { get; set; }
    }
}
