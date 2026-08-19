namespace GymManagement.Application.Exceptions
{
    /// <summary>
    /// The payment provider could not confirm an operation we must not proceed without — currently
    /// cancelling a recurring subscription. Maps to HTTP 502: the request was valid, our side is
    /// healthy, and the caller can retry once Mercado Pago answers again. Deliberately distinct from
    /// a generic Exception so nothing is persisted and the caller gets a message worth acting on.
    /// </summary>
    public class BillingUnavailableException : Exception
    {
        public BillingUnavailableException(string message) : base(message) { }
    }
}
