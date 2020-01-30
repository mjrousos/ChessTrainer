using System.Linq;
using System.Threading.Tasks;
using MjrChess.Trainer.Models;

namespace MjrChess.Trainer.Data
{
    public interface IRepository<T>
        where T : IEntity
    {
        Task<T?> GetAsync(int id);

        Task<IQueryable<T>> GetAllAsync();

        Task<T> AddAsync(T item);

        Task<T?> UpdateAsync(T item);

        Task<bool> DeleteAsync(int id);
    }
}
