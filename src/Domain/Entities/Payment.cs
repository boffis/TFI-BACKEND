namespace Domain.Entities
{
    public class Payment
    {
        public Guid IdPayment { get; set; }

        public Guid IdClient { get; set; }

        public required Client Client { get; set; }

        public decimal Price { get; set; }

        public DateTime PaymentDate { get; set; }

        public string PaymentMethod { get; set; } = string.Empty;

        public string PaymentState { get; set; } = string.Empty;    
    }
}
