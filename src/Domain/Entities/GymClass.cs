namespace GymManagement.Domain.Entities
{
    public class GymClass
    {
        public Guid GymClassId{ get; set; }

        public required string ClassName { get; set; }

        public string? ClassDescription { get; set; }

        public required int MaxCapacity { get; set; }

        public Guid TrainerId { get; set; }

        public required Trainer Trainer { get; set; }

        public required DateTime Schedule { get; set; }

        public ICollection<Inscription> Inscriptions { get; set; } = new List<Inscription>();
    }
}