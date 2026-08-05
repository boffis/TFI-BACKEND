namespace GymManagement.Application.Requests
{
    public class SubscriptionRequest
    {
        /// <summary>
        /// The card token obtained from the Mercado Pago Card Payment Brick on the frontend.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        public string IssuerId { get; set; } = string.Empty;

        public string PaymentMethodId { get; set; } = string.Empty;

        public SubscriptionPayerRequest Payer { get; set; } = new();

        public Guid MembershipPlanId { get; set; }
    }

    public class SubscriptionPayerRequest
    {
        public string Email { get; set; } = string.Empty;

        public SubscriptionPayerIdentification Identification { get; set; } = new();
    }

    public class SubscriptionPayerIdentification
    {
        public string Type { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
    }
}
