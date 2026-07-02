using GymManagement.Domain.Entities;
using GymManagement.Domain.Enums;

namespace GymManagement.Application.Requests
{
    public class MembershipRequest
    {
        public Guid ClientId { get; set; }

        public MembershipType MembershipType { get; set; }
    }
}
