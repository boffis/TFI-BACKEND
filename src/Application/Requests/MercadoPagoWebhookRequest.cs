namespace GymManagement.Application.Requests
{
    /// <summary>
    /// A Mercado Pago webhook payload — payment, subscription_preapproval and
    /// subscription_authorized_payment events alike.
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
