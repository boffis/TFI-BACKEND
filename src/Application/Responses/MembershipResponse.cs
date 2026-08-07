using System;
namespace GymManagement.Application.Responses
{
    public class MembershipResponse
    {
        public Guid MembershipId { get; set; }

        public Guid UserId { get; set; }

        public required MembershipPlanResponse MembershipPlan { get; set; }

        public DateTime ExpirationDate { get; set; }

        public bool IsCancelled { get; set; }
    }
}
