using System;
using System.Collections.Generic;
using System.Linq;
using GymManagement.Application.Interfaces;
using GymManagement.Domain.Entities;
using GymManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Infrastructure.Repositories
{
    public class GymClassScheduleRepository : IGymClassScheduleRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<GymClassSchedule> _dbSet;

        public GymClassScheduleRepository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.GymClassSchedules;
        }

        public List<GymClassSchedule> GetAll()
        {
            return [.. _dbSet.Include(s => s.Trainer).Where(s => !s.IsDeleted)];
        }

        public List<GymClassSchedule> GetActiveSchedules()
        {
            return [.. _dbSet.Include(s => s.Trainer).Where(s => s.IsActive && !s.IsDeleted)];
        }

        public GymClassSchedule? GetById(Guid id)
        {
            return _dbSet.Include(s => s.Trainer).FirstOrDefault(s => s.GymClassScheduleId == id && !s.IsDeleted);
        }

        public List<GymClassSchedule> GetByTrainerId(Guid trainerId)
        {
            return [.. _dbSet.Include(s => s.Trainer).Where(s => s.TrainerId == trainerId && !s.IsDeleted)];
        }

        public GymClassSchedule Add(GymClassSchedule schedule)
        {
            _dbSet.Add(schedule);
            _context.SaveChanges();
            return schedule;
        }

        public void Update(GymClassSchedule schedule)
        {
            _dbSet.Update(schedule);
            _context.SaveChanges();
        }

        public void Delete(Guid id)
        {
            var schedule = _dbSet.FirstOrDefault(s => s.GymClassScheduleId == id);
            if (schedule != null)
            {
                schedule.IsDeleted = true;
                _dbSet.Update(schedule);
                _context.SaveChanges();
            }
        }
    }
}
