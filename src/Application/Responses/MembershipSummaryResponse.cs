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
        public DateTime ExpirationDate { get; set; }
    }
}
