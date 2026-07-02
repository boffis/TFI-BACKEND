using GymManagement.Application.Interfaces;
using GymManagement.Application.Requests;
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

        public List<Admin> GetAllAdmins() => _adminRepository.GetAll();

        public Admin? GetAdminById(Guid id) => _adminRepository.GetById(id);

        public List<Admin> GetDeletedAdmins() => _adminRepository.GetDeleteds();

        public Admin? GetDeletedAdminById(Guid id) => _adminRepository.GetDeletedById(id);

        public bool UpdateAdmin(Guid id, UserRequest request)
        {
            var admin = _adminRepository.GetById(id);
            if (admin == null) return false;

            admin.Name = request.Name;
            admin.Email = request.Email;
            admin.Password = request.Password;

            _adminRepository.Update(admin);
            return true;
        }

        public bool DeleteAdmin(Guid id)
        {
            var admin = _adminRepository.GetById(id);
            if (admin == null) return false;

            _adminRepository.Delete(id);
            return true;
        }

        public bool RecoverAdmin(Guid id)
        {
            var admin = _adminRepository.GetDeletedById(id);
            if (admin == null) return false;

            _adminRepository.Recover(id);
            return true;
        }
    }
}