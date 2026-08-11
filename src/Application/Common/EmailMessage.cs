namespace GymManagement.Application.Common
{
    /// <summary>
    /// One outgoing email. Used with <c>IEmailService.SendBulkEmailAsync</c> so a batch can be
    /// personalised per recipient while still going out over a single SMTP connection.
    /// </summary>
    public class EmailMessage
    {
        public required string ToEmail { get; init; }

        public required string Subject { get; init; }

        public required string HtmlBody { get; init; }
    }
}
