using System;

namespace GymManagement.Application.Responses
{
    public class GymClassSummaryResponse
    {
        public Guid GymClassId { get; set; }
        public required string ClassName { get; set; }
        public required DateTime Schedule { get; set; }
        public bool IsClassDeleted { get; set; }
    }
}
