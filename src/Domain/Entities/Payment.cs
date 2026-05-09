namespace GymManagement.Domain.Entities
{
    public class Payment
    {
        public Guid PaymentId { get; set; }

        public Guid ClientId { get; set; }

        public required Client Client { get; set; }

        public decimal Price { get; set; }

        public DateTime PaymentDate { get; set; }

        public string PaymentMethod { get; set; } = string.Empty;

        public string PaymentState { get; set; } = string.Empty;    
    }
}
