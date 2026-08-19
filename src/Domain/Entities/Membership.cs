
namespace GymManagement.Domain.Entities
{
    public class Membership
    {
        public Guid MembershipId { get; set; }

        public Guid UserId { get; set; }
    
        public required User User { get; set; }

        public Guid MembershipPlanId { get; set; }

        public required MembershipPlan MembershipPlan { get; set; }

        public bool IsCancelled { get; set; } = false;

        /// <summary>
        /// False once the recurring charge was deliberately stopped while the membership runs on to
        /// ExpirationDate (an admin discontinued the plan). The webhook reads this to tell our own
        /// preapproval cancellation apart from a revocation.
        /// </summary>
        public bool AutoRenew { get; set; } = true;

        public DateTime ExpirationDate { get; set; }

        /// <summary>Mercado Pago preapproval id, stored so the subscription can be cancelled later.</summary>
        public string? MpPreapprovalId { get; set; }

        public ICollection<Payment> Payments { get; set; } = [];
    }
}
