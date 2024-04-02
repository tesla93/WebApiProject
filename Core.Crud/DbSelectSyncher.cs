using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Crud.Interfaces;
using Core.Data;
using Core.DTO;
using Core.Exceptions;

namespace Core.Crud
{
    /// <summary>
    /// A class to handle multiple ID selecting (or DTO list to synch) from UI and save them into DB. Created to reduce the code duplication.
    /// </summary>
    public class DbSelectSyncher<TEntity, TEntityDTO> : IDbSelectSynchable<TEntity, TEntityDTO>
        where TEntity : IEntity, ILookupSelectable, new()
        where TEntityDTO : IDTO, ILookupSelectable
    {
        private readonly IList<TEntity> _dbCollection;
        private readonly IList<TEntityDTO> _dtoCollection;
        private readonly string _lookupTable;

        public DbSelectSyncher(
                IList<TEntity> dbCollection,
                IList<TEntityDTO> dtoCollection,
                string collectionNameToUpdate
            )
        {
            this._dbCollection = dbCollection;
            this._dtoCollection = dtoCollection;
            this._lookupTable = collectionNameToUpdate;
        }

        public void SynchDbCollection(Action<TEntity> removeAction, Action<TEntity, TEntityDTO> addAction, Action<TEntity, TEntityDTO> updateAction)
        {
            try
            {
                this.RemoveAndUpdateDbCollection(removeAction, updateAction);
                var recordsToAdd = this.GetRecordsToAdd();
                foreach (var dto in recordsToAdd)
                {
                    this.AddDtoToDbCollection(dto, addAction);
                }
            }
            catch (Exception ex)
            {
                // TODO: logs
                throw new DbSyncherException($"Could not synch DB collection for {this._lookupTable}", ex);
            }
        }

        private void RemoveAndUpdateDbCollection(Action<TEntity> removeAction, Action<TEntity, TEntityDTO> updateAction)
        {
            foreach (var dbItem in this._dbCollection.Where(x => x.Id > 0))
            {
                // delete those DB records whose LookupId is not in the DTO list
                if (!this._dtoCollection.Select(x => x.LookupId).Contains(dbItem.LookupId))
                {
                    if (removeAction != null)
                    {
                        removeAction(dbItem);
                    }
                }
                else
                {
                    // Record found in DB and DTO. Update it.
                    var dtoItem = this._dtoCollection.Single(x => x.LookupId == dbItem.LookupId);
                    if (updateAction != null)
                    {
                        updateAction(dbItem, dtoItem);
                    }
                }
            }
        }


        private List<TEntityDTO> GetRecordsToAdd()
        {
            return this._dtoCollection
                .Where(dto => !this._dbCollection.Any(db => db.LookupId == dto.LookupId))
                .ToList();
        }

        private void AddDtoToDbCollection(TEntityDTO dto, Action<TEntity, TEntityDTO> addAction)
        {
            var entityToAdd = new TEntity();
            if (addAction != null)
            {
                addAction(entityToAdd, dto);
            }
        }
    }
}