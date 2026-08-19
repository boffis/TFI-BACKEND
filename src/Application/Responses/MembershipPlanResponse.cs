using System;

namespace GymManagement.Application.Responses
{
    public class MembershipPlanResponse
    {
        public Guid MembershipPlanId { get; set; }
        public string? Type { get; set; }
        public decimal Price { get; set; }
        public int DurationInDays { get; set; }

        /// <summary>Discontinued. Only ever true on the admin listing.</summary>
        public bool IsDeleted { get; set; }
    }
}
