using GymManagement.Domain.Enums;

namespace GymManagement.Application.Responses
{
    public class MembershipResponse
    {
        public Guid MembershipId { get; set; }

        public Guid ClientId { get; set; }

        public MembershipType MembershipType { get; set; }

        public bool MembershipState { get; set; }

        public DateTime ExpirationDate { get; set; }
    }
}