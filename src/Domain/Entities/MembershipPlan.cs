using System;

namespace GymManagement.Domain.Entities
{
    public class MembershipPlan
    {
        public Guid MembershipPlanId { get; set; }

        public string? Type { get; set; }

        public decimal Price { get; set; }

        public int DurationInDays { get; set; }
    }
}
