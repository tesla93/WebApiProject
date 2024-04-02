using System;
using System.Collections.Generic;
using Core.Data;
using Core.DTO;

namespace Core.Crud.Interfaces
{
    /// <summary>
    /// An interface to handle multiple DTO selecting from UI and save them into DB. Created to reduce the code duplication.
    /// </summary>
    /// <param name="removeAction">The hook to receive an entity and remove it from DB collection.</param>
    /// <param name="addAction">The hook to receive an empty entity to add to collection with DTO props.</param>
    public interface IDbSelectSynchable<TEntity, TEntityDTO>
        where TEntity : IEntity, new()
        where TEntityDTO : IDTO
    {
        void SynchDbCollection(Action<TEntity> removeAction, Action<TEntity, TEntityDTO> addAction, Action<TEntity, TEntityDTO> updateAction);
    }
}