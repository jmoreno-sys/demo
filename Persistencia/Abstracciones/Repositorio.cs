using Dominio.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Persistencia.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Persistencia.Abstracciones
{
    public abstract class Repositorio<TEntity> : Repository<TEntity, int>, IRepositorio<TEntity> where TEntity : class, IEntidad<int>
    {
        protected Repositorio(IDbContextFactory<ContextoPrincipal> contextFactory, ILoggerFactory logger)
            : base(contextFactory, logger) { }
    }

    public abstract class Repository<TEntity, TKey> : IRepositorio<TEntity, TKey> where TEntity : class, IEntidad<TKey> where TKey : struct, IComparable<TKey>, IEquatable<TKey>
    {
        protected readonly IDbContextFactory<ContextoPrincipal> ContextFactory;
        protected readonly ILogger Logger;

        protected Repository(IDbContextFactory<ContextoPrincipal> contextFactory, ILoggerFactory logger)
        {
            ContextFactory = contextFactory;
            Logger = logger.CreateLogger(GetType());
        }

        public virtual async Task<IList<TResult>> ReadAsync<TResult>(
            Expression<Func<TEntity, TResult>> selector = null,
            Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
            int? skip = 0, int? take = null,
            bool disableTracking = false,
            bool ignoreQueryFilters = false)
        {
            using var db = ContextFactory.CreateDbContext();
            IQueryable<TEntity> query = db.Set<TEntity>();
            if (disableTracking) query = query.AsNoTracking();
            if (include != null) query = include(query);
            if (predicate != null) query = query.Where(predicate);
            if (ignoreQueryFilters) query = query.IgnoreQueryFilters();
            if (skip > 0) query = query.Skip(skip.Value);
            if (take > 0) query = query.Take(take.Value);
            var result = orderBy != null ? orderBy(query).Select(selector) : query.Select(selector);
            return await result.ToListAsync();
        }

        public virtual IList<TResult> Read<TResult>(
            Expression<Func<TEntity, TResult>> selector = null,
            Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
            int? skip = 0, int? take = null,
            bool disableTracking = false,
            bool ignoreQueryFilters = false)
        {
            using var db = ContextFactory.CreateDbContext();
            IQueryable<TEntity> query = db.Set<TEntity>();
            if (disableTracking) query = query.AsNoTracking();
            if (include != null) query = include(query);
            if (predicate != null) query = query.Where(predicate);
            if (ignoreQueryFilters) query = query.IgnoreQueryFilters();
            if (skip > 0) query = query.Skip(skip.Value);
            if (take > 0) query = query.Take(take.Value);
            var result = orderBy != null ? orderBy(query).Select(selector) : query.Select(selector);
            return result.ToList();
        }

        public virtual async Task<IList<TResult>> SearchAsync<TResult>(Expression<Func<TEntity, TResult>> selector = null, string criteria = "", Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null, int? skip = 0, int? take = null, bool disableTracking = false, bool ignoreQueryFilters = false)
        {
            var searchs = criteria.Split(" ");
            var methodInfo = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            var parameter = Expression.Parameter(typeof(TEntity));
            var props = typeof(TEntity).GetProperties().Where(m => m.PropertyType == typeof(string) && m.GetCustomAttribute<NotMappedAttribute>() == null).ToArray();
            Expression predicate = null;
            foreach (var item in props)
                foreach (var search in searchs)
                    predicate = predicate == null
                        ? Expression.Call(Expression.Property(parameter, item.Name), methodInfo, Expression.Constant(search))
                        : Expression.OrElse(predicate, Expression.Call(Expression.Property(parameter, item.Name), methodInfo, Expression.Constant(search)));

            var expression = Expression.Lambda<Func<TEntity, bool>>(predicate, parameter);
            return await ReadAsync(selector, expression, orderBy, include, skip, take, disableTracking, ignoreQueryFilters);
        }

        public virtual IList<TResult> Search<TResult>(Expression<Func<TEntity, TResult>> selector = null, string criteria = "", Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null, int? skip = 0, int? take = null, bool disableTracking = false, bool ignoreQueryFilters = false)
        {
            var searchs = criteria.Split(" ");
            var methodInfo = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            var parameter = Expression.Parameter(typeof(TEntity));
            var props = typeof(TEntity).GetProperties().Where(m => m.PropertyType == typeof(string) && m.GetCustomAttribute<NotMappedAttribute>() == null).ToArray();
            Expression predicate = null;
            foreach (var item in props)
                foreach (var search in searchs)
                    predicate = predicate == null
                        ? Expression.Call(Expression.Property(parameter, item.Name), methodInfo, Expression.Constant(search))
                        : Expression.OrElse(predicate, Expression.Call(Expression.Property(parameter, item.Name), methodInfo, Expression.Constant(search)));

            var expression = Expression.Lambda<Func<TEntity, bool>>(predicate, parameter);
            return Read(selector, expression, orderBy, include, skip, take, disableTracking, ignoreQueryFilters);
        }

        public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate = null)
        {
            using var db = ContextFactory.CreateDbContext();
            return predicate != null ? await db.Set<TEntity>().CountAsync(predicate) : await db.Set<TEntity>().CountAsync();
        }

        public virtual int Count(Expression<Func<TEntity, bool>> predicate = null)
        {
            using var db = ContextFactory.CreateDbContext();
            return predicate != null ? db.Set<TEntity>().Count(predicate) : db.Set<TEntity>().Count();
        }

        public virtual async Task<long> LongCountAsync(Expression<Func<TEntity, bool>> predicate = null)
        {
            using var db = ContextFactory.CreateDbContext();
            return predicate != null ? await db.Set<TEntity>().LongCountAsync(predicate) : await db.Set<TEntity>().LongCountAsync();
        }

        public virtual long LongCount(Expression<Func<TEntity, bool>> predicate = null)
        {
            using var db = ContextFactory.CreateDbContext();
            return predicate != null ? db.Set<TEntity>().LongCount(predicate) : db.Set<TEntity>().LongCount();
        }

        public virtual async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate = null)
        {
            using var db = ContextFactory.CreateDbContext();
            return predicate != null ? await db.Set<TEntity>().AnyAsync(predicate) : await db.Set<TEntity>().AnyAsync();
        }

        public virtual bool Any(Expression<Func<TEntity, bool>> predicate = null)
        {
            using var db = ContextFactory.CreateDbContext();
            return predicate != null ? db.Set<TEntity>().Any(predicate) : db.Set<TEntity>().Any();
        }

        public virtual async Task CreateAsync(params TEntity[] entities)
        {
            using var db = ContextFactory.CreateDbContext();
            await db.Set<TEntity>().AddRangeAsync(entities);
            await db.SaveChangesAsync();
        }

        public virtual void Create(params TEntity[] entities)
        {
            using var db = ContextFactory.CreateDbContext();
            db.Set<TEntity>().AddRange(entities);
            db.SaveChanges();
        }

        public virtual async Task UpdateAsync(params TEntity[] entities)
        {
            using var db = ContextFactory.CreateDbContext();
            db.Set<TEntity>().UpdateRange(entities);
            await db.SaveChangesAsync();
        }

        public virtual void Update(params TEntity[] entities)
        {
            using var db = ContextFactory.CreateDbContext();
            db.Set<TEntity>().UpdateRange(entities);
            db.SaveChanges();
        }

        public virtual async Task DeleteAsync(params TKey[] keys)
        {
            using var db = ContextFactory.CreateDbContext();
            var list = db.Set<TEntity>().Where(m => keys.Contains(m.Id)).ToList();
            db.Set<TEntity>().RemoveRange(list);
            await db.SaveChangesAsync();
        }

        public virtual void Delete(params TKey[] keys)
        {
            using var db = ContextFactory.CreateDbContext();
            var list = db.Set<TEntity>().Where(m => keys.Contains(m.Id)).ToList();
            db.Set<TEntity>().RemoveRange(list);
            db.SaveChanges();
        }

        public virtual async Task CreateOrUpdateAsync(params TEntity[] entities)
        {
            foreach (var item in entities)
            {
                if (item.Id.Equals(default))
                    await CreateAsync(item);
                else
                    await UpdateAsync(item);
            }
        }

        public virtual void CreateOrUpdate(params TEntity[] entities)
        {
            foreach (var item in entities)
            {
                if (item.Id.Equals(default))
                    Create(item);
                else
                    Update(item);
            }
        }

        public virtual async Task<TResult> FirstOrDefaultAsync<TResult>(
            Expression<Func<TEntity, TResult>> selector = null,
            Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
            bool disableTracking = false,
            bool ignoreQueryFilters = false)
        {
            using var db = ContextFactory.CreateDbContext();
            IQueryable<TEntity> query = db.Set<TEntity>();
            if (disableTracking) query = query.AsNoTracking();
            if (include != null) query = include(query);
            if (predicate != null) query = query.Where(predicate);
            if (ignoreQueryFilters) query = query.IgnoreQueryFilters();
            var result = orderBy != null ? orderBy(query).Select(selector) : query.Select(selector);
            return await result.FirstOrDefaultAsync();
        }

        public virtual TResult FirstOrDefault<TResult>(
            Expression<Func<TEntity, TResult>> selector = null,
            Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
            bool disableTracking = false,
            bool ignoreQueryFilters = false)
        {
            using var db = ContextFactory.CreateDbContext();
            IQueryable<TEntity> query = db.Set<TEntity>();
            if (disableTracking) query = query.AsNoTracking();
            if (include != null) query = include(query);
            if (predicate != null) query = query.Where(predicate);
            if (ignoreQueryFilters) query = query.IgnoreQueryFilters();
            var result = orderBy != null ? orderBy(query).Select(selector) : query.Select(selector);
            return result.FirstOrDefault();
        }

        public virtual async Task<TResult> LastOrDefaultAsync<TResult>(
            Expression<Func<TEntity, TResult>> selector = null,
            Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
            bool disableTracking = false,
            bool ignoreQueryFilters = false)
        {
            using var db = ContextFactory.CreateDbContext();
            IQueryable<TEntity> query = db.Set<TEntity>();
            if (disableTracking) query = query.AsNoTracking();
            if (include != null) query = include(query);
            if (predicate != null) query = query.Where(predicate);
            if (ignoreQueryFilters) query = query.IgnoreQueryFilters();
            query = query.OrderByDescending(m => m.Id);
            var result = orderBy != null ? orderBy(query).Select(selector) : query.Select(selector);
            return await result.FirstOrDefaultAsync();
        }

        public virtual TResult LastOrDefault<TResult>(
            Expression<Func<TEntity, TResult>> selector = null,
            Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
            bool disableTracking = false,
            bool ignoreQueryFilters = false)
        {
            using var db = ContextFactory.CreateDbContext();
            IQueryable<TEntity> query = db.Set<TEntity>();
            if (disableTracking) query = query.AsNoTracking();
            if (include != null) query = include(query);
            if (predicate != null) query = query.Where(predicate);
            if (ignoreQueryFilters) query = query.IgnoreQueryFilters();
            query = query.OrderByDescending(m => m.Id);
            var result = orderBy != null ? orderBy(query).Select(selector) : query.Select(selector);
            return result.FirstOrDefault();
        }
    }
}
