namespace GymManagement.Application.Exceptions
{
    /// <summary>
    /// The payment provider couldn't confirm an operation we must not proceed without. Maps to
    /// HTTP 502, so the caller knows to retry once Mercado Pago answers again.
    /// </summary>
    public class BillingUnavailableException : Exception
    {
        public BillingUnavailableException(string message) : base(message) { }
    }
}
