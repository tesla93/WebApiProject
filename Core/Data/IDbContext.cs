using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Z.EntityFramework.Plus;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Core.Data
{
    public interface IDbContext: IDisposable
    {
        DatabaseFacade Database { get; }

        IModel Model { get; }

        DbSet<T> Set<T>() where T : class;

        DbQuery<T> Query<T>() where T : class;

        int SaveChanges();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        EntityEntry<TEntity> Entry<TEntity>(TEntity item) where TEntity : class;

        BaseQueryFilter Filter<T>(Func<IQueryable<T>, IQueryable<T>> queryFilter, bool isEnabled = true);
        BaseQueryFilter Filter<T>(object key, Func<IQueryable<T>, IQueryable<T>> queryFilter, bool isEnabled = true);

        Dictionary<PropertyInfo, Type> FindKeys(Type type);
    }
}
