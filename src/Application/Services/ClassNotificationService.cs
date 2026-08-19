using GymManagement.Application.Common;
using GymManagement.Application.Interfaces;
using GymManagement.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace GymManagement.Application.Services
{
    /// <inheritdoc cref="IClassNotificationService"/>
    public class ClassNotificationService : IClassNotificationService
    {
        private readonly IInscriptionRepository _inscriptionRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<ClassNotificationService> _logger;

        public ClassNotificationService(
            IInscriptionRepository inscriptionRepository,
            IEmailService emailService,
            ILogger<ClassNotificationService> logger)
        {
            _inscriptionRepository = inscriptionRepository;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task NotifyClassesCancelledAsync(IReadOnlyCollection<GymClass> cancelledClasses)
        {
            var messages = new List<EmailMessage>();

            foreach (var gymClass in cancelledClasses)
            {
                foreach (var client in EnrolledClients(gymClass.GymClassId))
                {
                    messages.Add(new EmailMessage
                    {
                        ToEmail = client.Email,
                        Subject = EmailTemplates.ClassCancelledSubject,
                        HtmlBody = EmailTemplates.ClassCancelled(
                            client.Name, gymClass.ClassName, gymClass.Schedule)
                    });
                }
            }

            await SendAsync(messages, "cancellation");
        }

        public async Task NotifyClassRescheduledAsync(GymClass gymClass, DateTime previousSchedule)
        {
            var messages = EnrolledClients(gymClass.GymClassId)
                .Select(client => new EmailMessage
                {
                    ToEmail = client.Email,
                    Subject = EmailTemplates.ClassRescheduledSubject,
                    HtmlBody = EmailTemplates.ClassRescheduled(
                        client.Name, gymClass.ClassName, previousSchedule, gymClass.Schedule)
                })
                .ToList();

            await SendAsync(messages, "reschedule");
        }

        /// <summary>
        /// Enrolled clients with a usable address. A null Client is a leftover from a role change,
        /// so there is nobody to write to.
        /// </summary>
        private List<Client> EnrolledClients(Guid gymClassId) =>
            [.. _inscriptionRepository.GetByClassId(gymClassId)
                .Select(i => i.Client)
                .Where(c => c != null && !string.IsNullOrWhiteSpace(c.Email))
                .Cast<Client>()];

        private async Task SendAsync(List<EmailMessage> messages, string kind)
        {
            if (messages.Count == 0) return;

            try
            {
                var sent = await _emailService.SendBulkEmailAsync(messages);
                _logger.LogInformation(
                    "[Notifications] Sent {Sent}/{Total} {Kind} emails", sent, messages.Count, kind);
            }
            catch (Exception ex)
            {
                // Swallowed deliberately: the class change is already applied and must not be
                // undone because SMTP was unreachable.
                _logger.LogError(ex,
                    "[Notifications] Could not send {Total} {Kind} emails", messages.Count, kind);
            }
        }
    }
}
