using Core.Data;
using Core.Filters;
using System;
using System.Linq;

namespace Core.Crud.Interfaces
{
    public interface IFilterSortService<TEntity, TKey>
        where TEntity : class, IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        IQueryable<TEntity> ApplyFilter(IQueryable<TEntity> query, Filter filter);
        IQueryable<TEntity> ApplySorting(IQueryable<TEntity> query, ISorter command);
    }
}