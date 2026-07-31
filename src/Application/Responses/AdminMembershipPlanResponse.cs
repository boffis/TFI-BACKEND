using System;
using System.Collections.Generic;

namespace GymManagement.Application.Responses
{
    public class AdminMembershipPlanResponse
    {
        public Guid MembershipPlanId { get; set; }
        public string? Type { get; set; }
        public decimal Price { get; set; }
        public int DurationInDays { get; set; }
        
        public ICollection<MembershipSummaryResponse> Memberships { get; set; } = [];
    }
}
