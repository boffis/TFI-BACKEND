namespace GymManagement.Domain.Entities
{
    public class Client : User
    {
        public Membership? Membership { get; set; }

        public ICollection<Inscription> Inscriptions { get; set; } = new List<Inscription>();
    }
}