namespace Domain.Entities
{
    public class GymClass
    {
        public int Guid { get; set; }

        public required string ClassName { get; set; }

        public string ClassDescription { get; set; } = string.Empty;

        public required int MaxCapacity { get; set; }

        public required string TrainerName { get; set; }

        public required DateTime Schedule { get; set; }   
    }
}
