namespace Domain.Entities
{
    public class Client : User
    {
        public bool MembershipState { get; set; }

        public required Membership Membership { get; set; }
    }
}
