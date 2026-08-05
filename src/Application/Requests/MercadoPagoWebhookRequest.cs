namespace GymManagement.Application.Requests
{
    /// <summary>
    /// Represents the Webhook notification payload sent by Mercado Pago.
    /// Supports both standard payment and subscription_preapproval / subscription_authorized_payment events.
    /// </summary>
    public class MercadoPagoWebhookRequest
    {
        public string? Action { get; set; }
        public string? ApiVersion { get; set; }
        public MercadoPagoWebhookData? Data { get; set; }
        public DateTime? DateCreated { get; set; }
        public long? Id { get; set; }
        public bool? LiveMode { get; set; }
        public string? Type { get; set; }
        public string? Topic { get; set; }
    }

    public class MercadoPagoWebhookData
    {
        public string? Id { get; set; }
    }
}
