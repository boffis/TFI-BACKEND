using GymManagement.Domain.Enums;

namespace GymManagement.Domain.Entities
{
    public class Inscription
    {
        public Guid InscriptionId { get; set; }

        public Guid? ClientId { get; set; }

        public Client? Client { get; set; }

        public Guid GymClassId { get; set; }

        public required GymClass GymClass { get; set; }

        /// <summary>
        /// Attendance is a property of the enrolment itself (one row per client per class),
        /// so it lives here rather than in a separate table.
        /// </summary>
        public AttendanceStatus AttendanceStatus { get; set; } = AttendanceStatus.NotRecorded;

        /// <summary>
        /// When attendance was last marked. Null while <see cref="AttendanceStatus"/> is
        /// <see cref="AttendanceStatus.NotRecorded"/>.
        /// </summary>
        public DateTime? AttendanceRecordedAt { get; set; }
    }
}
