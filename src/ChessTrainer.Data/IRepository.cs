using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MjrChess.Trainer.Models;

namespace MjrChess.Trainer.Data
{
    public interface IRepository<T>
        where T : IEntity
    {
        Task<T?> GetAsync(int id);

        IQueryable<T> Query();

        IQueryable<T> Query(Expression<Func<T, bool>>? filter);

        Task<T> AddAsync(T item);

        Task<T?> UpdateAsync(T item);

        Task<bool> DeleteAsync(int id);
    }
}
