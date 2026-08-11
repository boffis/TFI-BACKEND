using GymManagement.Application.Common;
using GymManagement.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Threading.Tasks;

namespace GymManagement.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var emailMessage = BuildMessage(toEmail, subject, body);

            using var client = new SmtpClient();
            await ConnectAsync(client);
            await client.SendAsync(emailMessage);
            await client.DisconnectAsync(true);
        }

        public async Task<int> SendBulkEmailAsync(IEnumerable<EmailMessage> messages)
        {
            var pending = messages.ToList();
            if (pending.Count == 0) return 0;

            var sent = 0;

            using var client = new SmtpClient();
            await ConnectAsync(client);

            foreach (var message in pending)
            {
                try
                {
                    await client.SendAsync(BuildMessage(message.ToEmail, message.Subject, message.HtmlBody));
                    sent++;
                }
                catch (Exception ex)
                {
                    // Keep going: the rest of the batch is unrelated to this address failing.
                    _logger.LogError(ex, "[Email] Could not send '{Subject}' to {Recipient}",
                        message.Subject, message.ToEmail);
                }
            }

            await client.DisconnectAsync(true);

            if (sent < pending.Count)
                _logger.LogWarning("[Email] Sent {Sent} of {Total} messages in batch", sent, pending.Count);

            return sent;
        }

        private MimeMessage BuildMessage(string toEmail, string subject, string body)
        {
            var fromAddress = _configuration["EmailSettings:FromAddress"];
            var fromName = _configuration["EmailSettings:FromName"] ?? "Gym Management";

            if (string.IsNullOrEmpty(fromAddress))
            {
                throw new System.Exception("Sender email address is not configured.");
            }

            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(fromName, fromAddress));
            emailMessage.To.Add(new MailboxAddress("", toEmail));
            emailMessage.Subject = subject;
            emailMessage.Body = new BodyBuilder { HtmlBody = body }.ToMessageBody();

            return emailMessage;
        }

        private async Task ConnectAsync(SmtpClient client)
        {
            var host = _configuration["EmailSettings:Host"];
            var port = int.TryParse(_configuration["EmailSettings:Port"], out int p) ? p : 587;
            var username = _configuration["EmailSettings:Username"];
            var password = _configuration["EmailSettings:Password"];

            if (string.IsNullOrEmpty(host))
            {
                throw new System.Exception("SMTP host is not configured.");
            }

            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                await client.AuthenticateAsync(username, password);
            }
        }
    }
}
