using Azure.Core;
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

        public User GetUserDeleted(Guid UserId)
        {
            return _adminRepository.GetUserDeleted(UserId);
        }

        public UserResponse CreateUser(UserRequest user)
        {
            var newUser = user.ToUser();

            _adminRepository.Add(newUser);

            return newUser.ToUserResponse();
        }

        public void UpdateUser(Guid UserId, UserRequest userRequest)
        {
            var user =  _adminRepository.GetUserById(UserId);
            if (user == null) return;
            user.Name = userRequest.Name;
            user.Email = userRequest.Email;
            user.Password = userRequest.Password;
            _adminRepository.Update(user);
        }

        public void DeleteUser(Guid UserId)
        {
            _adminRepository.Delete(UserId);
        }

        public void RecoverUser(Guid UserId)
        {
            _adminRepository.RecoverUser(UserId);
        }
    }
}
