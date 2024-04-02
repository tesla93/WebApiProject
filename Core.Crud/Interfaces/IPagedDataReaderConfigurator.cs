using Core.Data;
using System;
using System.Linq;

namespace Core.Crud.Interfaces
{
    internal interface IPagedDataReaderConfigurator<TEntity, TKey>
        where TEntity : class, IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        IQueryable<TEntity> GetQueryable();
        void SetQueryable(IQueryable<TEntity> entities);
    }
}