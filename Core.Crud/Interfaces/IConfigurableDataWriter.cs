using Core.Data;
using Core.DTO;
using System;

namespace Core.Crud.Interfaces
{
    internal interface IConfigurableDataWriter<TEntity, TEntityDTO, TKey>
        where TEntity : class, IEntity<TKey>
        where TEntityDTO : class, IDTO<TKey>
        where TKey : IEquatable<TKey>
    {
        void SetProvider(ISaveImplementationProvider<TEntity, TEntityDTO, TKey> provider);
    }
}