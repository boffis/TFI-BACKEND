
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
        /// False once the recurring charge has been deliberately stopped while the membership is
        /// still meant to run to its expiration date — currently only when an admin discontinues
        /// the plan. The Mercado Pago preapproval is cancelled at that moment, so the webhook that
        /// reports it must not read the cancellation as a revocation (see ProcessWebhookNotificationAsync).
        /// Access keeps working until ExpirationDate; nothing renews it afterwards.
        /// </summary>
        public bool AutoRenew { get; set; } = true;

        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// Mercado Pago Preapproval ID. Stored when the user subscribes so we can cancel it later.
        /// </summary>
        public string? MpPreapprovalId { get; set; }

        public ICollection<Payment> Payments { get; set; } = [];
    }
}
