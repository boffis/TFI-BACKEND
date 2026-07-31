namespace GymManagement.Domain.Entities
{
    public class Inscription
    {
        public Guid InscriptionId { get; set; }

        public Guid? ClientId { get; set; }

        public Client? Client { get; set; }

        public Guid GymClassId { get; set; }

        public required GymClass GymClass { get; set; }
    }
}
