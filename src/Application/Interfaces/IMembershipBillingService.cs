using System;
using System.Threading.Tasks;
using GymManagement.Domain.Entities;

namespace GymManagement.Application.Interfaces
{
    public interface IMembershipBillingService
    {
        /// <summary>
        /// Updates the recurring charge amount of an existing Mercado Pago preapproval
        /// (subscription) so the next billing cycle charges the new amount.
        /// Returns false (without throwing) if the update could not be applied — callers
        /// should treat this as best-effort and keep processing other subscribers.
        /// </summary>
        Task<bool> UpdatePreapprovalAmountAsync(string preapprovalId, decimal newAmount);

        /// <summary>
        /// Stops the recurring charge on an existing Mercado Pago preapproval while deliberately
        /// leaving the membership itself alone — the client keeps access until their current
        /// ExpirationDate, nothing renews it afterwards. Used when an admin discontinues a plan.
        /// Returns false (without throwing) if the preapproval could not be cancelled, so one
        /// stale subscriber doesn't abort the whole discontinue.
        /// </summary>
        Task<bool> StopAutoRenewalAsync(string preapprovalId);

        /// <summary>
        /// Admin-only path: cancels a membership and, if it's billed through Mercado Pago,
        /// cancels the underlying recurring preapproval too so future charges stop.
        /// </summary>
        Task AdminCancelSubscriptionAsync(Guid membershipId);

        /// <summary>
        /// Retires an already-expired membership that is being replaced, stopping its recurring
        /// charge first. An expired membership usually expired *because* its renewal charge kept
        /// failing, so its preapproval is typically still live at Mercado Pago — marking the row
        /// cancelled without cancelling that preapproval orphans a subscription that bills the
        /// client forever with nothing left able to clean it up.
        /// <para>
        /// Unlike the cancel paths above this keeps the client's future class inscriptions: the
        /// membership already lapsed on its own, and the caller is about to grant a replacement.
        /// </para>
        /// <para>
        /// Throws <see cref="Exceptions.BillingUnavailableException"/> if Mercado Pago cannot
        /// confirm the cancellation, leaving the membership untouched so the caller's whole
        /// operation fails and can be retried cleanly.
        /// </para>
        /// </summary>
        Task RetireSupersededMembershipAsync(Membership membership);
    }
}
