namespace GymManagement.Application.Common
{
    /// <summary>
    /// One outgoing email, so <c>IEmailService.SendBulkEmailAsync</c> can personalise a batch that
    /// still goes out over a single SMTP connection.
    /// </summary>
    public class EmailMessage
    {
        public required string ToEmail { get; init; }

        public required string Subject { get; init; }

        public required string HtmlBody { get; init; }
    }
}
