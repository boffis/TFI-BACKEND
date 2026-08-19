using System;
using System.Threading.Tasks;
using GymManagement.Domain.Entities;

namespace GymManagement.Application.Interfaces
{
    public interface IMembershipBillingService
    {
        /// <summary>
        /// Repoints a preapproval at a new amount for the next billing cycle. Best-effort:
        /// returns false instead of throwing so callers keep processing other subscribers.
        /// </summary>
        Task<bool> UpdatePreapprovalAmountAsync(string preapprovalId, decimal newAmount);

        /// <summary>
        /// Stops the recurring charge but leaves the membership alone: access runs to ExpirationDate
        /// and nothing renews it. Used when an admin discontinues a plan. Best-effort.
        /// </summary>
        Task<bool> StopAutoRenewalAsync(string preapprovalId);

        /// <summary>Admin: cancels a membership and its Mercado Pago preapproval, if any.</summary>
        Task AdminCancelSubscriptionAsync(Guid membershipId);

        /// <summary>
        /// Retires an expired membership being replaced, stopping its recurring charge first — its
        /// preapproval is usually still live and would otherwise bill forever. Keeps the client's
        /// future inscriptions, since a replacement is about to be granted. Throws
        /// <see cref="Exceptions.BillingUnavailableException"/> and changes nothing if MP can't confirm.
        /// </summary>
        Task RetireSupersededMembershipAsync(Membership membership);
    }
}
