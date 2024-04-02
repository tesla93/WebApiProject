using Core.Data;
using Core.DTO;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Crud.Interfaces
{
    internal interface ISaveImplementationProvider<TEntity, TEntityDTO, TKey>
        where TEntity : class, IEntity<TKey>
        where TEntityDTO : class, IDTO<TKey>
        where TKey : IEquatable<TKey>
    {
        Task<TEntity> Save(TEntityDTO entityDTO, TEntity entity, CancellationToken cancellationToken);
    }
}