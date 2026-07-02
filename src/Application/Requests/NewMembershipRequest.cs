using GymManagement.Domain.Enums;

namespace GymManagement.Application.Requests
{
    public class NewMembershipRequest
    {
        public MembershipType MembershipType { get; set; }
    }
}