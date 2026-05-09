namespace GymManagement.Domain.Entities
{
    public class Client : User
    {
        public required Membership Membership { get; set; }

    }
}
    