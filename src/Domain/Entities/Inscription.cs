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

        /// <summary>Attendance belongs to the enrolment itself, so no separate table.</summary>
        public AttendanceStatus AttendanceStatus { get; set; } = AttendanceStatus.NotRecorded;

        /// <summary>When attendance was last marked; null while the status is NotRecorded.</summary>
        public DateTime? AttendanceRecordedAt { get; set; }
    }
}
