using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.Extensions.ExpressionMapping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MjrChess.Trainer.Models;

namespace MjrChess.Trainer.Data
{
    public class EFRepository<TData, T> : IRepository<T>
        where T : IEntity
        where TData : Data.Models.EntityBase
    {
        protected PuzzleDbContext Context { get; }

        public IMapper Mapper { get; }

        private ILogger<EFRepository<TData, T>> Logger { get; }

        private DbSet<TData> DbSet => Context.Set<TData>();

        protected virtual IQueryable<TData> DbSetWithRelatedEntities => DbSet;

        public EFRepository(PuzzleDbContext context, IMapper mapper, ILogger<EFRepository<TData, T>> logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            // Disable this for now because it's awkward for services to have to begin tracking individual
            // entities they select from a query when they're used as children of other new entities.
            // In most cases, queries end up being small so this shouldn't matter. In the future, a stricter
            // service/repository boundary will enable this to be re-enabled.
            // Context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public virtual async Task<T> AddAsync(T item)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var dataItem = Mapper.Map<TData>(item);
            var result = await DbSet.AddAsync(dataItem);
            await Context.SaveChangesAsync();
            Logger.LogInformation("Added item {Id} to database {EntityType}", result.Entity.Id, typeof(TData).Name);

            return Mapper.Map<T>(result.Entity);
        }

        public virtual async Task<bool> DeleteAsync(int id)
        {
            var result = false;
            var entity = await DbSet.SingleOrDefaultAsync(t => t.Id == id);
            if (entity != null)
            {
                DbSet.Remove(entity);
                await Context.SaveChangesAsync();
                result = true;
            }

            Logger.LogInformation("Deleting entity {Id} from database {EntityType} {Result}", id, typeof(TData).Name, result ? "succeeded" : "failed");

            return result;
        }

        public virtual IQueryable<T> Query()
        {
            Logger.LogInformation("Querying items from database {EntityType}", typeof(TData).Name);
            return Mapper.ProjectTo<T>(DbSetWithRelatedEntities);
        }

        public virtual IQueryable<T> Query(Expression<Func<T, bool>>? filter)
        {
            Logger.LogInformation("Querying items from database {EntityType}", typeof(TData).Name);

            var query = DbSetWithRelatedEntities;
            if (filter != null)
            {
                var dataFilter = Mapper.MapExpression<Expression<Func<TData, bool>>>(filter);
                query = query.Where(dataFilter);
            }

            return Mapper.ProjectTo<T>(query);
        }

        public virtual async Task<T?> GetAsync(int id)
        {
            var entity = await DbSetWithRelatedEntities.SingleOrDefaultAsync(t => t.Id == id);
            Logger.LogInformation("Retrieving entity {Id} from database {EntityType} {Result}", id, typeof(TData).Name, entity == null ? "failed" : "succeeded");
            return Mapper.Map<T>(entity);
        }

        public virtual async Task<T?> UpdateAsync(T item)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            TData? ret = null;
            var entity = await DbSetWithRelatedEntities.SingleOrDefaultAsync(t => t.Id == item.Id);
            if (entity != null)
            {
                Mapper.Map(item, entity);
                var result = DbSet.Update(entity);
                await Context.SaveChangesAsync();
                ret = result.Entity;
            }

            Logger.LogInformation("Updating entity {Id} from database {EntityType} {Result}", item.Id, typeof(T).Name, ret != null ? "succeeded" : "failed");
            return Mapper.Map<T>(ret);
        }
    }
}
