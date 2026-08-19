using GymManagement.Domain.Enums;

namespace GymManagement.Application.Requests
{
    /// <summary>One roster submission: every row the trainer changed, in a single request.</summary>
    public class AttendanceRequest
    {
        public List<AttendanceEntryRequest> Entries { get; set; } = [];
    }

    public class AttendanceEntryRequest
    {
        public Guid ClientId { get; set; }

        public AttendanceStatus Status { get; set; }
    }
}
