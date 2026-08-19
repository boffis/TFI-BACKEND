using GymManagement.Application.Common;

namespace GymManagement.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);

        /// <summary>
        /// Sends many emails over one SMTP connection. <see cref="SendEmailAsync"/> pays a full
        /// handshake per message (~1-2s), which cancelling a weekly schedule would multiply into
        /// minutes. A failure on one message is logged and skipped, never abandoning the batch.
        /// </summary>
        /// <returns>How many messages were sent successfully.</returns>
        Task<int> SendBulkEmailAsync(IEnumerable<EmailMessage> messages);
    }
}
