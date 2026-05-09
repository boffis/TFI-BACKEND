using GymManagement.Application.Interfaces;
using GymManagement.Application.Mappers;
using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Domain.Entities;

namespace GymManagement.Application.Services
{
    public class AdminService 
    {
        private readonly IAdminRepository _adminRepository;

        public AdminService(IAdminRepository adminRepository) 
        {
            _adminRepository = adminRepository;        
        }

        public List<User> GetAllUsers()
        {
            return _adminRepository.GetAllUsers();
        }

        public User GetUserById(Guid UserId)
        {
            return _adminRepository.GetUserById(UserId);
        }

        public UserResponse CreateUser(UserRequest user)
        {
            var newUser = user.ToUser();

            _adminRepository.Add(newUser);

            return newUser.ToUserResponse();
        }
        }
}
