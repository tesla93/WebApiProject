using System;
using Core.Data;
using Microsoft.EntityFrameworkCore;

namespace Core.Crud.Interfaces
{
    public interface IODataReader<TEntity, TKey>
        where TEntity : class, IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        DbSet<TEntity> Set { get; }
    }

    public interface IODataReader<TEntity> : IODataReader<TEntity, int>
        where TEntity : class, IEntity<int>, IEntity
    {
    }
}
