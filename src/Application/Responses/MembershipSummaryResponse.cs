using System;

namespace GymManagement.Application.Responses
{
    public class MembershipSummaryResponse
    {
        public Guid MembershipId { get; set; }
        public Guid ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty;
        public bool IsCancelled { get; set; }

        /// <summary>False once the recurring charge was stopped and the membership is just running out.</summary>
        public bool AutoRenew { get; set; }
        public DateTime ExpirationDate { get; set; }
    }
}
