namespace GymManagement.Application.Requests
{
    public class MembershipRequest
    {
        public Guid UserId { get; set; }

        public Guid MembershipPlanId { get; set; }
    }
}
