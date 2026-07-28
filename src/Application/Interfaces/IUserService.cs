using GymManagement.Application.Requests;
using GymManagement.Application.Responses;

namespace GymManagement.Application.Interfaces
{
    public interface IUserService
    {
        UserResponse? GetById(Guid id);
        UserResponse? GetDeletedById(Guid id);
        List<UserResponse> GetAll();
        List<UserResponse> GetAllDeleted();
        bool Update(Guid id, UserRequest request);
        bool Delete(Guid id);
        bool Recover(Guid id);
        bool ChangeRole(Guid id, string newRole, string? specialization = null);
    }
}
