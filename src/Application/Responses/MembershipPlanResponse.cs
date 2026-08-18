using System;

namespace GymManagement.Application.Responses
{
    public class MembershipPlanResponse
    {
        public Guid MembershipPlanId { get; set; }
        public string? Type { get; set; }
        public decimal Price { get; set; }
        public int DurationInDays { get; set; }

        /// <summary>
        /// True when the plan has been discontinued. Only ever true on the admin listing — the
        /// public endpoints filter discontinued plans out entirely.
        /// </summary>
        public bool IsDeleted { get; set; }
    }
}
