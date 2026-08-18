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
            => await _dbSet.Include(m => m.MembershipPlan).ToListAsync();

        public async Task<Membership?> GetMembershipById(Guid membershipId)
            => await _dbSet.Include(m => m.MembershipPlan)
                           .FirstOrDefaultAsync(m => m.MembershipId == membershipId);

        public async Task<List<Membership>> GetByUserId(Guid userId)
            => await _dbSet.Include(m => m.MembershipPlan)
                           .Where(m => m.UserId == userId)
                           .ToListAsync();

        public async Task<Membership?> GetActiveByUserId(Guid userId)
            => await _dbSet.Include(m => m.MembershipPlan)
                           .FirstOrDefaultAsync(m => m.UserId == userId && !m.IsCancelled);

        // No Include of MembershipPlan: callers of this overload only need UserId/ExpirationDate.
        // AsNoTracking because the results are read-only — nothing here is written back.
        public async Task<List<Membership>> GetAllActive()
            => await _dbSet.AsNoTracking()
                           .Where(m => !m.IsCancelled)
                           .ToListAsync();

        public async Task<List<Membership>> GetByPlanId(Guid planId)
            => await _dbSet.Include(m => m.User)
                           .Where(m => m.MembershipPlanId == planId)
                           .ToListAsync();

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

        public async Task SetAutoRenewAsync(Membership membership, bool autoRenew)
        {
            membership.AutoRenew = autoRenew;
            _context.Entry(membership).Property(m => m.AutoRenew).IsModified = true;
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
