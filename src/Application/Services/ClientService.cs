using GymManagement.Application.Interfaces;
using GymManagement.Application.Requests;
using GymManagement.Domain.Entities;

namespace GymManagement.Application.Services
{
    public class ClientService
    {
        private readonly IClientRepository _clientRepository;
        public ClientService(IClientRepository clientRepository)
        {
            _clientRepository = clientRepository;
        }

        public List<Client> GetAllClients() => _clientRepository.GetAll();

        public Client? GetClientById(Guid id) => _clientRepository.GetById(id);

        public List<Client> GetDeletedClients() => _clientRepository.GetDeleteds();

        public Client? GetDeletedClientById(Guid id) => _clientRepository.GetDeletedById(id);

        public bool UpdateClient(Guid id, UserRequest request)
        {
            var client = _clientRepository.GetById(id);
            if (client == null) return false;

            client.Name = request.Name;
            client.Email = request.Email;
            client.Password = request.Password;

            _clientRepository.Update(client);
            return true;
        }

        public bool DeleteClient(Guid id)
        {
            var client = _clientRepository.GetById(id);
            if (client == null) return false;

            _clientRepository.Delete(id);
            return true;
        }

        public bool RecoverClient(Guid id)
        {
            var client = _clientRepository.GetDeletedById(id);
            if (client == null) return false;

            _clientRepository.Recover(id);
            return true;
        }
    }
}