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
        protected PuzzleDbContext Context { get; }

        private ILogger<EFRepository<T>> Logger { get; }

        private DbSet<T> DbSet => Context.Set<T>();

        protected virtual IQueryable<T> DbSetWithRelatedEntities => DbSet;

        public EFRepository(PuzzleDbContext context, ILogger<EFRepository<T>> logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Context = context ?? throw new ArgumentNullException(nameof(context));

            // Disable this for now because it's awkward for services to have to begin tracking individual
            // entities they select from a query when they're used as children of other new entities.
            // In most cases, queries end up being small so this shouldn't matter. In the future, a stricter
            // service/repository boundary will enable this to be re-enabled.
            // Context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public virtual async Task<T> AddAsync(T item)
        {
            var result = await DbSet.AddAsync(item);
            await Context.SaveChangesAsync();
            Logger.LogInformation("Added item {Id} to database {EntityType}", result.Entity.Id, typeof(T).Name);

            return result.Entity;
        }

        public virtual async Task<bool> DeleteAsync(int id)
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

        public virtual Task<IQueryable<T>> GetAllAsync()
        {
            Logger.LogInformation("Querying items from database {EntityType}", typeof(T).Name);
            return Task.FromResult(DbSetWithRelatedEntities.AsQueryable());
        }

        public virtual async Task<T?> GetAsync(int id)
        {
            var entity = await DbSetWithRelatedEntities.SingleOrDefaultAsync(t => t.Id == id);
            Logger.LogInformation("Retrieving entity {Id} from database {EntityType} {Result}", id, typeof(T).Name, entity == null ? "failed" : "succeeded");
            return entity;
        }

        public virtual async Task<T?> UpdateAsync(T item)
        {
            T? ret = null;
            var entity = await GetAsync(item.Id);
            if (entity != null)
            {
                var result = DbSet.Update(item);
                await Context.SaveChangesAsync();
                ret = result.Entity;
            }

            Logger.LogInformation("Updating entity {Id} from database {EntityType} {Result}", item.Id, typeof(T).Name, ret != null ? "succeeded" : "failed");
            return ret;
        }
    }
}
