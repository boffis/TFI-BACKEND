using GymManagement.Application.Interfaces;
using GymManagement.Application.Requests;
using GymManagement.Domain.Entities;
using GymManagement.Domain.Enums;

namespace GymManagement.Application.Services
{
    public class AdminService 
    {
        private readonly IAdminRepository _adminRepository;
        private readonly ITrainerRepository _trainerRepository;

        public AdminService(IAdminRepository adminRepository, ITrainerRepository trainerRepository) 
        {
            _adminRepository = adminRepository;
            _trainerRepository = trainerRepository;
        }

        public List<User> GetAllUsers()
        {
            return _adminRepository.GetAll();
        }

        public User? GetUserById(Guid UserId)
        {
            return _adminRepository.GetById(UserId);
        }

        public User? GetUserDeleted(Guid UserId)
        {
            return _adminRepository.GetUserDeleted(UserId);
        }

        public bool UpdateUser(Guid UserId, UserRequest userRequest)
        {
            var user =  _adminRepository.GetById(UserId);
            if (user == null) return false;
            user.Name = userRequest.Name;
            user.Email = userRequest.Email;
            user.Password = userRequest.Password;
            _adminRepository.Update(user);
            return true;
        }

        public bool DeleteUser(Guid UserId)
        {
            var user = _adminRepository.GetById(UserId);
            if (user == null) return false;

            _adminRepository.Delete(UserId);
            return true;
        }

        public bool RecoverUser(Guid UserId)
        {
            var user = _adminRepository.GetUserDeleted(UserId);
            if (user == null) return false;

            _adminRepository.Recover(UserId);
            return true;
        }
    }
}