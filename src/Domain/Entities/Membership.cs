
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

        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// Mercado Pago Preapproval ID. Stored when the user subscribes so we can cancel it later.
        /// </summary>
        public string? MpPreapprovalId { get; set; }

        public ICollection<Payment> Payments { get; set; } = [];
    }
}
