namespace GymManagement.Infrastructure.Settings;

public class MercadoPagoSettings
{
    public string AccessToken { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// Full URL of the webhook endpoint to pass to Mercado Pago when creating a preapproval.
    /// Example: https://tfi-api-e5fnasf3gfedakdf.chilecentral-01.azurewebsites.net/Payment/webhook
    /// </summary>
    public string WebhookUrl { get; set; } = string.Empty;
}
