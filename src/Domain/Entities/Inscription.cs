namespace Domain.Entities
{
    public class Inscription
    {
        public Guid ClientId { get; set; }
        public required Client Client { get; set; }
        public Guid GymClassId { get; set; }
        public required GymClass GymClass { get; set; }
        public DateTime ClassDate { get; set; } 
    }
}
