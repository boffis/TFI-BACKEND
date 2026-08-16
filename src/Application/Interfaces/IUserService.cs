using GymManagement.Application.Requests;
using GymManagement.Application.Responses;

namespace GymManagement.Application.Interfaces
{
    public interface IUserService
    {
        UserResponse? GetById(Guid id);
        Task<UserDetailedResponse?> GetDetailedByIdAsync(Guid id);
        UserResponse? GetDeletedById(Guid id);
        Task<GetAllUsersResponse> GetAllAsync();
        List<UserResponse> GetAllDeleted();
        bool Update(Guid id, UserRequest request);
        Task<bool> DeleteAsync(Guid id);
        bool Recover(Guid id);
        Task<bool> ChangeRoleAsync(Guid id, string newRole, string? specialization = null);
        List<ActiveTrainerResponse> GetActiveTrainers();
    }
}
