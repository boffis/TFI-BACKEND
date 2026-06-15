using GymManagement.Application.Interfaces;
using GymManagement.Domain.Entities;
using GymManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Infrastructure.Repositories
{
    public class GymClassRepository : IGymClassRepository
    {
        private readonly ApplicationDbContext _context;

        public GymClassRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<GymClass> GetAll()
        {
            return [.. _context.GymClasses.Where(gc => !gc.IsClassDeleted)];
        }

        public GymClass? GetById(Guid id)
        {
            return _context.GymClasses
                .Include(gc => gc.Trainer)
                .FirstOrDefault(gc => gc.GymClassId == id && !gc.IsClassDeleted);
        }

        public List<GymClass> GetByTrainerId(Guid trainerId)
        {
            return [.. _context.GymClasses
                .Where(gc => gc.TrainerId == trainerId && !gc.IsClassDeleted)];
        }

        public GymClass Add(GymClass gymClass)
        {
            var newGymClass = _context.GymClasses.Add(gymClass);
            _context.SaveChanges();
            return newGymClass.Entity;
        }

        public void Update(GymClass gymClass)
        {
            _context.GymClasses.Update(gymClass);
            _context.SaveChanges();
        }

        public void Delete(Guid id)
        {
            var gymClass = _context.GymClasses
                .FirstOrDefault(gc => gc.GymClassId == id && !gc.IsClassDeleted);
            if (gymClass != null)
            {
                gymClass.IsClassDeleted = true;
                _context.GymClasses.Update(gymClass);
                _context.SaveChanges();
            }
        }

        public void Recover(Guid id)
        {
            var gymClass = _context.GymClasses
                .FirstOrDefault(gc => gc.GymClassId == id);
            if (gymClass != null)
            {
                gymClass.IsClassDeleted = false;
                _context.GymClasses.Update(gymClass);
                _context.SaveChanges();
            }
        }

        public void Delete(GymClass gymClass)
        {
            gymClass.IsClassDeleted = true;
            _context.GymClasses.Update(gymClass);
            _context.SaveChanges();
        }
    }
}