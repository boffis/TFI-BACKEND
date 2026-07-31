using System;

namespace GymManagement.Application.Responses
{
    public class GymClassdto
    {
        public Guid GymClassId { get; set; }
        public required string ClassName { get; set; }
        public string? ClassDescription { get; set; }
        public required int MaxCapacity { get; set; }
        public Guid TrainerId { get; set; }
        public string TrainerName { get; set; }
        public required DateTime Schedule { get; set; }
        public Guid? GymClassScheduleId { get; set; }
        public bool IsClassDeleted { get; set; } = false;
        public int InscriptionAmount { get; set; } = 0;
    }
}
