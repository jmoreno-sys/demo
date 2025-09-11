using Dominio.Interfaces;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Persistencia.Interfaces
{
    public interface IRepositorio<TEntity> : IRepositorio<TEntity, int> where TEntity : class, IEntidad<int>
    {
    }

    public interface IRepositorio<TEntity, TKey> where TEntity : class, IEntidad<TKey> where TKey : struct, IComparable<TKey>, IEquatable<TKey>
    {
        Task<IList<TResult>> ReadAsync<TResult>(
            Expression<Func<TEntity, TResult>> selector = null,
            Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
            int? skip = 0, int? take = null,
            bool disableTracking = false,
            bool ignoreQueryFilters = false);

        IList<TResult> Read<TResult>(
            Expression<Func<TEntity, TResult>> selector = null,
            Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
            int? skip = 0, int? take = null,
            bool disableTracking = false,
            bool ignoreQueryFilters = false);

        Task<IList<TResult>> SearchAsync<TResult>(
           Expression<Func<TEntity, TResult>> selector = null,
           string criteria = "",
           Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
           Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
           int? skip = 0, int? take = null,
           bool disableTracking = false,
           bool ignoreQueryFilters = false);

        IList<TResult> Search<TResult>(
           Expression<Func<TEntity, TResult>> selector = null,
           string criteria = "",
           Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
           Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
           int? skip = 0, int? take = null,
           bool disableTracking = false,
           bool ignoreQueryFilters = false);

        Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate = null);

        int Count(Expression<Func<TEntity, bool>> predicate = null);

        Task<long> LongCountAsync(Expression<Func<TEntity, bool>> predicate = null);

        long LongCount(Expression<Func<TEntity, bool>> predicate = null);

        bool Any(Expression<Func<TEntity, bool>> predicate = null);

        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate = null);

        Task CreateAsync(params TEntity[] entity);

        void Create(params TEntity[] entity);

        Task CreateOrUpdateAsync(params TEntity[] entity);

        void CreateOrUpdate(params TEntity[] entity);

        Task UpdateAsync(params TEntity[] entity);

        void Update(params TEntity[] entity);

        Task DeleteAsync(params TKey[] key);

        void Delete(params TKey[] key);

        // * //

        Task<TResult> FirstOrDefaultAsync<TResult>(
            Expression<Func<TEntity, TResult>> selector = null,
            Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
            bool disableTracking = false,
            bool ignoreQueryFilters = false);

        TResult FirstOrDefault<TResult>(
            Expression<Func<TEntity, TResult>> selector = null,
            Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
            bool disableTracking = false,
            bool ignoreQueryFilters = false);

        Task<TResult> LastOrDefaultAsync<TResult>(
           Expression<Func<TEntity, TResult>> selector = null,
           Expression<Func<TEntity, bool>> predicate = null,
           Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
           Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
           bool disableTracking = false,
           bool ignoreQueryFilters = false);

        TResult LastOrDefault<TResult>(
            Expression<Func<TEntity, TResult>> selector = null,
            Expression<Func<TEntity, bool>> predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
            bool disableTracking = false,
            bool ignoreQueryFilters = false);


    }
}
