using Core.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Crud.Interfaces
{
    internal interface IDataRemoverConfigurator<TEntity, TKey>
        where TEntity : class, IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        void SetPreprocessor(Func<TEntity, CancellationToken, Task> processBeforeRemove);
    }
}