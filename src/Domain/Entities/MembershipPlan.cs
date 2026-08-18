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
        /// Soft-delete flag. A discontinued plan can no longer be bought or assigned, and is
        /// hidden from the public plan list, but its row survives so the memberships and payments
        /// that reference it keep their history. Admins can restore it.
        /// </summary>
        public bool IsDeleted { get; set; } = false;
    }
}
