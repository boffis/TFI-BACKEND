using GymManagement.Application.Interfaces;
using GymManagement.Domain.Entities;
using GymManagement.Infrastructure.Persistence;


namespace GymManagement.Infrastructure.Repositories
{
    public class AdminRepository : BaseRepository<Admin>, IAdminRepository
    {
        public AdminRepository(ApplicationDbContext context) : base(context) { }

        public List<Admin> GetAllAdmins()
        {
            return GetAll();
        }

        public Admin? GetAdminById(Guid id)
        {
            return GetById(id);
        }

        public List<Admin> GetDeletedAdmins()
        {
            return GetDeleteds();
        }

        public Admin? GetDeletedAdminById(Guid id)
        {
            return GetDeletedById(id);
        }

        public Admin AddAdmin(Admin admin)
        {
            return Add(admin);
        }

        public void UpdateAdmin(Admin admin)
        {
            Update(admin);
        }

        public void DeleteAdmin(Guid id)
        {
            Delete(id);
        }

        public void RecoverAdmin(Guid id)
        {
            Recover(id);
        }
    }
}
