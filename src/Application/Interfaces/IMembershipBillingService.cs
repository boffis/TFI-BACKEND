using System.Threading.Tasks;

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
    }
}
