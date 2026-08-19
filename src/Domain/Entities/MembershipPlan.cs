using System;

namespace GymManagement.Domain.Entities
{
    public class MembershipPlan
    {
        public Guid MembershipPlanId { get; set; }

        public string? Type { get; set; }

        public decimal Price { get; set; }

        public int DurationInDays { get; set; }

        /// <summary>
        /// Soft delete: the plan can no longer be bought and is hidden publicly, but its row
        /// survives so referencing memberships and payments keep their history. Restorable.
        /// </summary>
        public bool IsDeleted { get; set; } = false;
    }
}
