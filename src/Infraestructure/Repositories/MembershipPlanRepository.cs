using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GymManagement.Application.Interfaces;
using GymManagement.Domain.Entities;
using GymManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Infrastructure.Repositories
{
    public class MembershipPlanRepository : IMembershipPlanRepository
    {
        private readonly ApplicationDbContext _context;

        public MembershipPlanRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MembershipPlan>> GetAllAsync()
        {
            return await _context.MembershipPlans.ToListAsync();
        }

        public async Task<MembershipPlan?> GetByIdAsync(Guid id)
        {
            return await _context.MembershipPlans.FindAsync(id);
        }

        public async Task<MembershipPlan> AddAsync(MembershipPlan plan)
        {
            _context.MembershipPlans.Add(plan);
            await _context.SaveChangesAsync();
            return plan;
        }

        public async Task UpdateAsync(MembershipPlan plan)
        {
            _context.MembershipPlans.Update(plan);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var plan = await _context.MembershipPlans.FindAsync(id);
            if (plan != null)
            {
                _context.MembershipPlans.Remove(plan);
                await _context.SaveChangesAsync();
            }
        }
    }
}
