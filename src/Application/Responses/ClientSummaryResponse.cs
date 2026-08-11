using System;
using GymManagement.Domain.Enums;

namespace GymManagement.Application.Responses
{
    public class ClientSummaryResponse
    {
        public Guid ClientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Defaults to <see cref="AttendanceStatus.NotRecorded"/>, so callers that predate
        /// attendance (the admin class detail page) keep working unchanged.
        /// </summary>
        public AttendanceStatus AttendanceStatus { get; set; } = AttendanceStatus.NotRecorded;

        public DateTime? AttendanceRecordedAt { get; set; }
    }
}
