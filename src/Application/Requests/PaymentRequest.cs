namespace GymManagement.Application.Requests
{
    public class PaymentRequest
    {
        public Guid MembershipId { get; set; }

        public decimal Price { get; set; } = 0;
    }
}