using GymManagement.Application.Interfaces;
using GymManagement.Domain.Entities;
using GymManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Infrastructure.Repositories
{
    public class MembershipRepository : IMembershipRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<Membership> _dbSet;

        public MembershipRepository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Memberships;
        }

        public async Task<List<Membership>> GetAllMemberships()
            => await _dbSet.ToListAsync();

        public async Task<Membership?> GetMembershipById(Guid membershipId)
            => await _dbSet.FirstOrDefaultAsync(m => m.MembershipId == membershipId);

        public async Task<Membership> AddMembership(Membership membership)
        {
            await _dbSet.AddAsync(membership);
            await _context.SaveChangesAsync();
            return membership;
        }

        public async Task ChangeMembership(Membership membership)
        {
            _dbSet.Update(membership);
            await _context.SaveChangesAsync();
        }

        public async Task CancelMembership(Guid membershipId)
        {
            var membership = await GetMembershipById(membershipId);
            if (membership != null)
            {
                membership.IsCancelled = true;
                _dbSet.Update(membership);
                await _context.SaveChangesAsync();
            }
        }
    }
}