using GymManagement.Domain.Entities;

namespace GymManagement.Application.Interfaces
{
    /// <summary>
    /// Tells enrolled clients when something happens to a class they booked.
    /// <para>
    /// Every method here is best-effort: failures are logged and swallowed, never rethrown. A
    /// cancellation must not fail because the mail server is unreachable — the admin's action is
    /// the primary outcome, and the notification is a courtesy on top of it.
    /// </para>
    /// </summary>
    public interface IClassNotificationService
    {
        /// <summary>
        /// Emails everyone enrolled in the given classes that they were cancelled.
        /// <para>
        /// Call this <b>before</b> deleting, while the enrolment rows are still readable — this
        /// reads the roster itself rather than taking a pre-built recipient list.
        /// </para>
        /// </summary>
        Task NotifyClassesCancelledAsync(IReadOnlyCollection<GymClass> cancelledClasses);

        /// <summary>
        /// Emails everyone enrolled in a class that its date or time moved.
        /// </summary>
        Task NotifyClassRescheduledAsync(GymClass gymClass, DateTime previousSchedule);
    }
}
