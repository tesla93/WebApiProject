using System;
using Core.Data;
using AutofacExtensions;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Core.Services
{
    public abstract class DataService : IDataService
    {
        private readonly IDbContext context;

        protected DataService(IDbContext context) => this.context = context;

        [IgnoreLogging]
        public void ConfigureDataContext(Action<IDbContext> action) => action?.Invoke(context);

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            await context.SaveChangesAsync(cancellationToken);

        protected IQueryable<TEntity> GetQueryable<TEntity>() where TEntity : class
            => context.Set<TEntity>();

        protected async Task AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken) where TEntity : class
        {
            await context.Set<TEntity>().AddAsync(entity, cancellationToken);
        }

        protected void Update<TEntity>(TEntity entity) where TEntity : class
        {
            context.Set<TEntity>().Update(entity);
        }

        protected void Remove<TEntity>(TEntity entity) where TEntity : class =>
            context.Set<TEntity>().Remove(entity);

        protected void RemoveRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class =>
            context.Set<TEntity>().RemoveRange(entities);

        protected async Task AddRangeAsync<TEntity>(IEnumerable<TEntity> entities, CancellationToken cancellationToken) where TEntity : class =>
            await context.Set<TEntity>().AddRangeAsync(entities, cancellationToken);

        protected async Task<int> ExecuteSqlCommandAsync(string command, IEnumerable<object> parameters, CancellationToken cancellationToken) =>
            await context.Database.ExecuteSqlRawAsync(command, parameters, cancellationToken);
    }
}