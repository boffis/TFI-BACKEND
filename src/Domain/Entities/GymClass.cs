namespace GymManagement.Domain.Entities
{
    public class GymClass
    {
        public Guid GymClassId{ get; set; }

        public required string ClassName { get; set; }

        public string? ClassDescription { get; set; }

        public required int MaxCapacity { get; set; }

        public Guid TrainerId { get; set; }

        public required User Trainer { get; set; }

        public required DateTime Schedule { get; set; }

        public Guid? GymClassScheduleId { get; set; }

        public GymClassSchedule? GymClassSchedule { get; set; }

        public bool IsClassDeleted { get; set; } = false;

        public ICollection<Inscription> Inscriptions { get; set; } = [];
    }
}
