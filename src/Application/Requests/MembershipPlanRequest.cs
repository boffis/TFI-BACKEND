using System;

namespace GymManagement.Application.Requests
{
    public class MembershipPlanRequest
    {
        public string? Type { get; set; }

        public decimal Price { get; set; }

        public int DurationInDays { get; set; }
    }
}
