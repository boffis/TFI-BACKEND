using GymManagement.Application.Interfaces;
using GymManagement.Domain.Entities;
using GymManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Infrastructure.Repositories
{
    public class InscriptionRepository : IInscriptionRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<Inscription> _dbSet;

        public InscriptionRepository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Inscriptions;
        }

        public int CountByClassId(Guid classId)
        {
            return _dbSet.Count(i => i.GymClassId == classId);
        }

        public bool IsUserRepeated(Guid clientId, Guid classId)
        {
            return _dbSet.Any(i => i.ClientId == clientId && i.GymClassId == classId);
        }

        public Inscription Add(Inscription inscription)
        {
            _dbSet.Add(inscription);
            _context.SaveChanges();
            return inscription;
        }

        public void Remove(Guid clientId, Guid classId)
        {
            var inscription = _dbSet.FirstOrDefault(i => i.ClientId == clientId && i.GymClassId == classId);
            if (inscription != null)
            {
                _dbSet.Remove(inscription);
                _context.SaveChanges();
            }
        }

        public List<Inscription> GetByClassId(Guid classId)
        {
            return [.. _dbSet
                .Include(i => i.Client)
                .Where(i => i.GymClassId == classId)];
        }
    }
}

