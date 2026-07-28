using GymManagement.Application.Interfaces;
using GymManagement.Application.Mappers;
using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Domain.Entities;

namespace GymManagement.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IClientRepository _clientRepository;
        private readonly ITrainerRepository _trainerRepository;
        private readonly IAdminRepository _adminRepository;

        public UserService(
            IClientRepository clientRepository,
            ITrainerRepository trainerRepository,
            IAdminRepository adminRepository)
        {
            _clientRepository = clientRepository;
            _trainerRepository = trainerRepository;
            _adminRepository = adminRepository;
        }

        public List<UserResponse> GetAll()
        {
            var users = new List<UserResponse>();
            users.AddRange(_clientRepository.GetAll().Select(c => c.ToUserResponse()));
            users.AddRange(_trainerRepository.GetAll().Select(t => TrainerMapper.ToTrainerResponse(t)));
            users.AddRange(_adminRepository.GetAll().Select(a => a.ToUserResponse()));
            return users;
        }

        public List<UserResponse> GetAllDeleted()
        {
            var users = new List<UserResponse>();
            users.AddRange(_clientRepository.GetDeleteds().Select(c => c.ToUserResponse()));
            users.AddRange(_trainerRepository.GetDeleteds().Select(t => TrainerMapper.ToTrainerResponse(t)));
            users.AddRange(_adminRepository.GetDeleteds().Select(a => a.ToUserResponse()));
            return users;
        }

        public UserResponse? GetById(Guid id)
        {
            var user = GetUserEntityById(id);
            if (user == null) return null;

            if (user is Trainer trainer) return TrainerMapper.ToTrainerResponse(trainer);
            return user.ToUserResponse();
        }

        public UserResponse? GetDeletedById(Guid id)
        {
            var user = GetDeletedUserEntityById(id);
            if (user == null) return null;

            if (user is Trainer trainer) return TrainerMapper.ToTrainerResponse(trainer);
            return user.ToUserResponse();
        }

        public bool Update(Guid id, UserRequest request)
        {
            var user = GetUserEntityById(id);
            if (user == null) return false;

            user.Name = request.Name;
            user.Email = request.Email;
            user.Password = request.Password;
            user.DateOfBirth = request.DateOfBirth;
            user.DNI = request.DNI;
            user.Gender = request.Gender;
            user.PhoneNumber = request.PhoneNumber;

            if (user is Client client) _clientRepository.Update(client);
            else if (user is Trainer trainer) _trainerRepository.Update(trainer);
            else if (user is Admin admin) _adminRepository.Update(admin);

            return true;
        }

        public bool Delete(Guid id)
        {
            var user = GetUserEntityById(id);
            if (user == null) return false;

            if (user is Client) _clientRepository.Delete(id);
            else if (user is Trainer) _trainerRepository.Delete(id);
            else if (user is Admin) _adminRepository.Delete(id);

            return true;
        }

        public bool Recover(Guid id)
        {
            var user = GetDeletedUserEntityById(id);
            if (user == null) return false;

            if (user is Client) _clientRepository.Recover(id);
            else if (user is Trainer) _trainerRepository.Recover(id);
            else if (user is Admin) _adminRepository.Recover(id);

            return true;
        }

        public bool ChangeRole(Guid id, string newRole, string? specialization = null)
        {
            var user = GetUserEntityById(id);
            if (user == null) return false;

            var currentRole = user.GetType().Name;
            if (currentRole.Equals(newRole, StringComparison.OrdinalIgnoreCase)) return true;

            // Delete from old repo permanently? 
            // We need a hard delete for this to move them, or we can just soft delete? 
            // Better to hard delete so we don't have duplicates with the same UserId.
            // Wait, BaseRepository only does soft delete. We need a hard delete method or we can just use DbContext directly in a repo.
            // Wait, changing roles across TPT tables by deleting and inserting requires DbContext.
            // For now, I will use soft delete? No, if we soft delete the Client, and add a Trainer with same ID, EF Core might allow it because they are separate tables, but it's bad design.
            // Since we need to access DbContext, maybe I should do this in AuthService or add a HardDelete method. Let's just use the current repositories for now. Wait, I can't hard delete easily.
            // I'll throw an exception for now or implement a HardDelete.
            throw new NotImplementedException("Changing role requires hard-delete support in repositories.");
        }

        private User? GetUserEntityById(Guid id)
        {
            var client = _clientRepository.GetById(id);
            if (client != null) return client;

            var trainer = _trainerRepository.GetById(id);
            if (trainer != null) return trainer;

            var admin = _adminRepository.GetById(id);
            if (admin != null) return admin;

            return null;
        }

        private User? GetDeletedUserEntityById(Guid id)
        {
            var client = _clientRepository.GetDeletedById(id);
            if (client != null) return client;

            var trainer = _trainerRepository.GetDeletedById(id);
            if (trainer != null) return trainer;

            var admin = _adminRepository.GetDeletedById(id);
            if (admin != null) return admin;

            return null;
        }
    }
}
