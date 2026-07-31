using GymManagement.Application.Interfaces;
using GymManagement.Domain.Entities;
using GymManagement.Infrastructure.Persistence;

namespace GymManagement.Infrastructure.Repositories
{
    public class ClientRepository : BaseRepository<Client>, IClientRepository
    {
        public ClientRepository(ApplicationDbContext context) : base(context)
        {
        }
        public List<Client> GetAllClients() => GetAll();

        public Client? GetClientById(Guid id) => GetById(id);

        public List<Client> GetDeletedClients() => GetDeleteds();

        public Client? GetDeletedClientById(Guid id) => GetDeletedById(id);

        public Client AddClient(Client client) => Add(client);

        public void UpdateClient(Client client) => Update(client);

        public void DeleteClient(Guid id) => Delete(id);

        public void RecoverClient(Guid id) => Recover(id);
    }
}
