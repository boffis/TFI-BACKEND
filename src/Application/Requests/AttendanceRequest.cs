using GymManagement.Domain.Enums;

namespace GymManagement.Application.Requests
{
    /// <summary>
    /// One roster submission: the trainer marks the whole class at once and saves,
    /// so the client sends every row it wants to change in a single request.
    /// </summary>
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
