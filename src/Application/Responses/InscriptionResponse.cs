using GymManagement.Domain.Enums;

namespace GymManagement.Application.Responses
{
    public class InscriptionResponse
    {
        public Guid InscriptionId { get; set; }
        public Guid GymClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public DateTime Schedule { get; set; }
        public AttendanceStatus AttendanceStatus { get; set; } = AttendanceStatus.NotRecorded;
        public DateTime? AttendanceRecordedAt { get; set; }
    }
}
