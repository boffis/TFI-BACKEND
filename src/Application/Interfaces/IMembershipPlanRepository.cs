using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GymManagement.Domain.Entities;

namespace GymManagement.Application.Interfaces
{
    public interface IMembershipPlanRepository
    {
        /// <summary>Live plans only — what clients are allowed to see and buy.</summary>
        Task<IEnumerable<MembershipPlan>> GetAllAsync();

        /// <summary>Admin view: live plans plus discontinued ones, so they can be reviewed or restored.</summary>
        Task<IEnumerable<MembershipPlan>> GetAllIncludingDiscontinuedAsync();

        /// <summary>
        /// Returns the plan whether or not it is discontinued — existing memberships, the admin
        /// detail page and payment history all need to read discontinued plans back. Callers that
        /// must refuse a discontinued plan (new purchases, plan changes) check IsDeleted themselves.
        /// </summary>
        Task<MembershipPlan?> GetByIdAsync(Guid id);

        Task<MembershipPlan> AddAsync(MembershipPlan plan);
        Task UpdateAsync(MembershipPlan plan);

        /// <summary>Soft delete: flags the plan as discontinued, keeping the row and its history.</summary>
        Task DeleteAsync(Guid id);

        /// <summary>Clears the discontinued flag, making the plan purchasable again.</summary>
        Task RestoreAsync(Guid id);
    }
}
