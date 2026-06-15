using System;

namespace GymManagement.Application.Responses
{
    public class GymClassResponse
    {
        public Guid GymClassId { get; set; }

        public string ClassName { get; set; } = string.Empty;

        public string? ClassDescription { get; set; }

        public int MaxCapacity { get; set; }

        public Guid TrainerId { get; set; }

        public DateTime Schedule { get; set; }
    }
}
