using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GymManagement.Domain.Entities;

namespace GymManagement.Application.Interfaces
{
    public interface IMembershipPlanRepository
    {
        Task<IEnumerable<MembershipPlan>> GetAllAsync();
        Task<MembershipPlan?> GetByIdAsync(Guid id);
        Task<MembershipPlan> AddAsync(MembershipPlan plan);
        Task UpdateAsync(MembershipPlan plan);
        Task DeleteAsync(Guid id);
    }
}
