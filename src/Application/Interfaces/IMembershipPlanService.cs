using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GymManagement.Application.Requests;
using GymManagement.Application.Responses;

namespace GymManagement.Application.Interfaces
{
    public interface IMembershipPlanService
    {
        Task<IEnumerable<MembershipPlanResponse>> GetAllPlansAsync();
        Task<MembershipPlanResponse> GetPlanByIdAsync(Guid id);
        Task<AdminMembershipPlanResponse> GetAdminPlanByIdAsync(Guid id);
        Task<MembershipPlanResponse> CreatePlanAsync(MembershipPlanRequest request);
        Task<MembershipPlanResponse> UpdatePlanAsync(Guid id, MembershipPlanRequest request);
        Task DeletePlanAsync(Guid id);
    }
}
