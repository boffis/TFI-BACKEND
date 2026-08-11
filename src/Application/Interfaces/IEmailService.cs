using GymManagement.Application.Common;

namespace GymManagement.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);

        /// <summary>
        /// Sends many emails over a single SMTP connection.
        /// <para>
        /// <see cref="SendEmailAsync"/> connects, authenticates and disconnects per message, which
        /// costs a full handshake each time (~1-2s against Gmail). Cancelling a weekly schedule can
        /// mean one message per enrolled client per upcoming session, so sending those one at a time
        /// would run for minutes inside a single HTTP request.
        /// </para>
        /// <para>
        /// A failure on one message is logged and skipped rather than abandoning the batch — one bad
        /// address shouldn't stop everyone else being told their class was cancelled.
        /// </para>
        /// </summary>
        /// <returns>How many messages were sent successfully.</returns>
        Task<int> SendBulkEmailAsync(IEnumerable<EmailMessage> messages);
    }
}
