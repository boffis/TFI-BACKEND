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
            inscription.InscriptionId = Guid.NewGuid();
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

        public void RemoveById(Guid inscriptionId)
        {
            var inscription = _dbSet.FirstOrDefault(i => i.InscriptionId == inscriptionId);
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

        public List<Inscription> GetByClientId(Guid clientId)
        {
            return [.. _dbSet
                .Include(i => i.GymClass)
                .Where(i => i.ClientId == clientId)];
        }

        public void NullifyClientId(Guid inscriptionId)
        {
            var inscription = _dbSet.FirstOrDefault(i => i.InscriptionId == inscriptionId);
            if (inscription != null)
            {
                inscription.ClientId = null;
                _dbSet.Update(inscription);
                _context.SaveChanges();
            }
        }
    }
}
