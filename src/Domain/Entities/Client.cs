namespace GymManagement.Domain.Entities
{
    public class Client : User
    {
        public ICollection<Inscription> Inscriptions { get; set; } = new List<Inscription>();
    }
}
