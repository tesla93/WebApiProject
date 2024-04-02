using AutoMapper;
using Core.Crud.Interfaces;
using Core.Data;
using Core.DTO;
using Core.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Crud
{
    public class DataWriter<TEntity, TEntityDTO, TKey> : DataService, IDataWriter<TEntityDTO, TKey>, IConfigurableDataWriter<TEntity, TEntityDTO, TKey>
        where TEntity : class, IEntity<TKey>
        where TEntityDTO : class, IDTO<TKey>
        where TKey : IEquatable<TKey>
    {
        private ISaveImplementationProvider<TEntity, TEntityDTO, TKey> provider;
        public DataWriter(IDbContext context, IMapper mapper) : base(context)
        {
            Mapper = mapper;
        }
        protected IMapper Mapper { get; }

        public async Task<TEntityDTO> Save(TEntityDTO dto, bool saveChanges = true, CancellationToken cancellationToken = default)
        {
            TEntity entity = await SaveInternal(dto, cancellationToken);
            if (entity == null)
            {
                dto.Id = default(TKey);
                entity = await Create(dto, cancellationToken);
            }

            if (provider != null)
            {
                await provider.Save(dto, entity, cancellationToken);
            }

            if (saveChanges)
            {
                await SaveChangesAsync(cancellationToken);
            }

            return Mapper.Map<TEntityDTO>(entity);
        }

        public async Task<TEntityDTO> Save(TEntityDTO dto, CancellationToken cancellationToken) =>
            await Save(dto, true, cancellationToken);

        private async Task<TEntity> SaveInternal(TEntityDTO dto, CancellationToken cancellationToken)
        {
            if (Equals(dto.Id, default(TKey)))
            {
                return await Create(dto, cancellationToken);
            }
            
            return await Update(dto, cancellationToken);
        }

        protected virtual async Task<TEntity> Create(TEntityDTO dto, CancellationToken cancellationToken)
        {
            var entity = Mapper.Map<TEntity>(dto);
            await AddAsync(entity, cancellationToken);
            return entity;
        }

        protected virtual async Task<TEntity> Update(TEntityDTO dto, CancellationToken cancellationToken)
        {
            var entity = await GetQueryable<TEntity>().FirstOrDefaultAsync(x => x.Id.Equals(dto.Id), cancellationToken);
            Mapper.Map(dto, entity);
            return entity;
        }

        void IConfigurableDataWriter<TEntity, TEntityDTO, TKey>.SetProvider(ISaveImplementationProvider<TEntity, TEntityDTO, TKey> provider)
        {
            this.provider = provider;
        }
    }
}