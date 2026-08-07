namespace GymManagement.Application.Responses
{
    public class PaymentResponse
    {
        public Guid PaymentId { get; set; }

        public Guid UserId { get; set; }

        public Guid MembershipId { get; set; }

        public decimal Price { get; set; }

        public DateTime PaymentDate { get; set; }

        public string PaymentMethod { get; set; } = string.Empty;

        public string PaymentState { get; set; } = string.Empty;
    }
}
