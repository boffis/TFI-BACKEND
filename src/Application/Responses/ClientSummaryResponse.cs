using System;
using GymManagement.Domain.Enums;

namespace GymManagement.Application.Responses
{
    public class ClientSummaryResponse
    {
        public Guid ClientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        /// <summary>Defaults to NotRecorded, so callers that predate attendance keep working.</summary>
        public AttendanceStatus AttendanceStatus { get; set; } = AttendanceStatus.NotRecorded;

        public DateTime? AttendanceRecordedAt { get; set; }
    }
}
