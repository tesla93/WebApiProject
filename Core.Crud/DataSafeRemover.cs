using Core.Crud.Interfaces;
using Core.Data;
using Core.Data.Attributes;
using Core.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Crud
{
    public class DataSafeRemover<TEntity, TKey> : DataService, IDataRemover<TKey>, IDataRemoverConfigurator<TEntity, TKey>
        where TEntity : class, IEntity<TKey>, ISafeRemovable
        where TKey : IEquatable<TKey>
    {
        private Func<TEntity, CancellationToken, Task> preprocessor = (TEntity, CancellationToken) => Task.CompletedTask;
        public DataSafeRemover(IDbContext context) : base(context)
        {
        }

        public async Task Delete(TKey id, CancellationToken cancellationToken = default)
        {
            var entity = await GetQueryable<TEntity>().FirstOrDefaultAsync(x => x.Id.Equals(id), cancellationToken);
            if (entity != null)
            {
                await ProcessBeforeRemoveInternal(entity, cancellationToken);
                entity.IsRemoved = true;
                entity.RemovedOn = DateTime.UtcNow; // should be service here
                await SaveChangesAsync(cancellationToken);
            }
        }

        public async Task DeleteAll(CancellationToken cancellationToken = default)
        {
            if (!Attribute.IsDefined(typeof(TEntity), typeof(AllowDeleteAllAttribute))) return;
            var entities = GetQueryable<TEntity>();
            await entities.ForEachAsync(async entity => {
                await ProcessBeforeRemoveInternal(entity, cancellationToken);
                entity.IsRemoved = true;
                entity.RemovedOn = DateTime.UtcNow; // should be service here
            });
            await SaveChangesAsync(cancellationToken);
        }

        public void SetPreprocessor(Func<TEntity, CancellationToken, Task> preprocessor)
        {
            this.preprocessor = preprocessor;
        }

        protected virtual Task ProcessBeforeRemove(TEntity entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        private async Task ProcessBeforeRemoveInternal(TEntity entity, CancellationToken cancellationToken = default)
        {
            await ProcessBeforeRemove(entity, cancellationToken);
            await preprocessor(entity, cancellationToken);
        }
    }
}