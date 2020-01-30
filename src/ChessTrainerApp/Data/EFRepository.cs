using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MjrChess.Trainer.Models;

namespace MjrChess.Trainer.Data
{
    public class EFRepository<T> : IRepository<T>
        where T : IEntity
    {
        private PuzzleDbContext Context { get; }

        private ILogger<EFRepository<T>> Logger { get; }

        private DbSet<T> DbSet => Context.Set<T>();

        public EFRepository(PuzzleDbContext context, ILogger<EFRepository<T>> logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public async Task<T> AddAsync(T item)
        {
            var result = await DbSet.AddAsync(item);
            await Context.SaveChangesAsync();
            Logger.LogInformation("Added item {Id} to database {EntityType}", result.Entity.Id, typeof(T).Name);

            return result.Entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var result = false;
            var entity = await GetAsync(id);
            if (entity != null)
            {
                DbSet.Remove(entity);
                await Context.SaveChangesAsync();
                result = true;
            }

            Logger.LogInformation("Deleting entity {Id} from database {EntityType} {Result}", id, typeof(T).Name, result ? "succeeded" : "failed");

            return result;
        }

        public Task<IQueryable<T>> GetAllAsync()
        {
            Logger.LogInformation("Querying items from database {EntityType}", typeof(T).Name);
            return Task.FromResult(DbSet.AsQueryable());
        }

        public async Task<T?> GetAsync(int id)
        {
            var entity = await DbSet.SingleOrDefaultAsync(t => t.Id == id);
            Logger.LogInformation("Retrieving entity {Id} from database {EntityType} {Result}", id, typeof(T).Name, entity == null ? "failed" : "succeeded");
            return entity;
        }

        public async Task<T?> UpdateAsync(T item)
        {
            T? ret = null;
            var entity = await GetAsync(item.Id);
            if (entity != null)
            {
                var result = DbSet.Update(item);
                ret = result.Entity;
            }

            Logger.LogInformation("Updating entity {Id} from database {EntityType} {Result}", item.Id, typeof(T).Name, ret != null ? "succeeded" : "failed");
            return ret;
        }
    }
}
