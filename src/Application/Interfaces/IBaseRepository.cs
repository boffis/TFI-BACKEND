using System;
using System.Collections.Generic;
using System.Text;

namespace GymManagement.Application.Interfaces
{
    public interface IBaseRepository<T> where T : class
    {
        List<T> GetAll();

        T? GetById(Guid id);

        T Add(T entity);

        void Update(T entity);

        void Delete(Guid id);

        void Recover(Guid id);
    }
}
