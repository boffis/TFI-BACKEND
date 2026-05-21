using GymManagement.Domain.Enums;

namespace GymManagement.Domain.Entities
{
    public class Membership
    {
        public Guid MembershipId { get; set; }

        public Guid ClientId { get; set; }
    
        public required Client Client { get; set; }

        public MembershipType MembershipType { get; set; }

        public bool MembershipState { get; set; }

        public DateTime ExpirationDate { get; set; }

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
