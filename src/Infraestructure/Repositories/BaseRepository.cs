using GymManagement.Application.Interfaces;
using GymManagement.Infrastructure.Persistence;
using GymManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Infrastructure.Repositories
{
    public class BaseRepository<T> : IBaseRepository<T> where T : User
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;
    

    public BaseRepository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual List<T> GetAll()
        {
            return [.. _dbSet.Where(x => !x.IsUserDeleted)];
        }

        public virtual T? GetById(Guid id)
        {
            return _dbSet.FirstOrDefault(x => x.UserId == id && !x.IsUserDeleted);
        }

        public virtual T Add(T entity)
        {
            _dbSet.Add(entity);
            _context.SaveChanges();
            return entity;
        }

        public virtual void Update(T entity)
        {
            _dbSet.Update(entity);
            _context.SaveChanges();
        }

        public virtual void Delete(Guid id)
        {
            var entity = GetById(id);
            if (entity != null)
            {
                entity.IsUserDeleted = true;
                _dbSet.Update(entity);
                _context.SaveChanges();
            }

        }

        public virtual void Recover(Guid id)
        {
            var entity = GetById(id);
            if (entity != null)
            {
                entity.IsUserDeleted = false;
                _dbSet.Update(entity);
                _context.SaveChanges();
            }
        }
    }
}
