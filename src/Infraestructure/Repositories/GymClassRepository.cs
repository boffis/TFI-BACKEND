using GymManagement.Application.Interfaces;
using GymManagement.Domain.Entities;
using GymManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Infrastructure.Repositories
{
    public class GymClassRepository : IGymClassRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<GymClass> _dbSet;

        public GymClassRepository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.GymClasses;
        }

        public List<GymClass> GetAll()
        {
            return [.. _dbSet.Include(gc => gc.Trainer).Where(gc => !gc.IsClassDeleted)];
        }

        public GymClass? GetById(Guid id)
        {
            return _dbSet
                .Include(gc => gc.Trainer)
                .FirstOrDefault(gc => gc.GymClassId == id && !gc.IsClassDeleted);
        }

        public List<GymClass> GetByTrainerId(Guid trainerId)
        {
            return [.. _dbSet
                .Where(gc => gc.TrainerId == trainerId && !gc.IsClassDeleted)];
        }

        public GymClass Add(GymClass gymClass)
        {
            _dbSet.Add(gymClass);
            _context.SaveChanges();
            return gymClass;
        }

        public void Update(GymClass gymClass)
        {
            _dbSet.Update(gymClass);
            _context.SaveChanges();
        }

        public void Delete(Guid id)
        {
            var gymClass = _dbSet.FirstOrDefault(gc => gc.GymClassId == id && !gc.IsClassDeleted);
            if (gymClass != null)
            {
                gymClass.IsClassDeleted = true;
                _dbSet.Update(gymClass);
                _context.SaveChanges();
            }
        }

        public void Recover(Guid id)
        {
            var gymClass = _dbSet.FirstOrDefault(gc => gc.GymClassId == id);
            if (gymClass != null)
            {
                gymClass.IsClassDeleted = false;
                _dbSet.Update(gymClass);
                _context.SaveChanges();
            }
        }

        public List<GymClass> GetDeleted()
        {
            return [.. _dbSet.Where(gc => gc.IsClassDeleted)];
        }

        public GymClass? GetDeletedById(Guid id)
        {
            return _dbSet.FirstOrDefault(gc => gc.GymClassId == id && gc.IsClassDeleted);
        }

        public bool Exists(Guid gymClassScheduleId, DateTime scheduleDateTime)
        {
            return _dbSet.Any(gc => gc.GymClassScheduleId == gymClassScheduleId 
                                 && gc.Schedule == scheduleDateTime 
                                 && !gc.IsClassDeleted);
        }
    }
}
