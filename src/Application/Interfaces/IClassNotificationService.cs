using GymManagement.Domain.Entities;

namespace GymManagement.Application.Interfaces
{
    /// <summary>
    /// Tells enrolled clients when something happens to a class they booked. Best-effort: failures
    /// are logged and swallowed, so an unreachable mail server never fails the admin's action.
    /// </summary>
    public interface IClassNotificationService
    {
        /// <summary>
        /// Emails enrolled clients that their classes were cancelled. Call <b>before</b> deleting:
        /// this reads the roster itself rather than taking a recipient list.
        /// </summary>
        Task NotifyClassesCancelledAsync(IReadOnlyCollection<GymClass> cancelledClasses);

        /// <summary>Emails enrolled clients that a class moved.</summary>
        Task NotifyClassRescheduledAsync(GymClass gymClass, DateTime previousSchedule);
    }
}
