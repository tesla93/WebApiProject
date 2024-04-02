using AutoMapper;
using AutoMapper.QueryableExtensions;
using Core.Crud.Interfaces;
using Core.Data;
using Core.DTO;
using Core.Filters;
using Core.Model;
using Core.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Crud
{
    public class DataReader<TEntity, TEntityDTO, TKey> : DataService, IDataReader<TEntityDTO, TKey>, IDataReaderConfigurator<TEntity, TKey>
        where TEntity : class, IEntity<TKey>
        where TEntityDTO : class, IDTO<TKey>
        where TKey : IEquatable<TKey>
    {
        public DataReader(IDbContext dbContext, IMapper mapper, bool useMappingOnDb = false) : base(dbContext)
        {
            Mapper = mapper;
            UseMappingOnDb = useMappingOnDb;
            InitialQuery = GetQueryable<TEntity>();
        }

        protected IMapper Mapper { get; }
        public bool UseMappingOnDb { get; set; }

        public async Task<bool> Exists(TKey id, CancellationToken cancellationToken = default)
            => await GetQueryable(false, true).AnyAsync(entityItem => entityItem.Id.Equals(id), cancellationToken);

        public async Task<bool> Exists(Expression<Func<TEntityDTO, bool>> expression, CancellationToken cancellationToken = default)
            => await GetQueryable(false, true).AnyAsync(ExpressionTransformer.Tranform<TEntityDTO, TEntity>(expression, Mapper), cancellationToken);

        public async Task<TEntityDTO> Get(Expression<Func<TEntityDTO, bool>> expression, CancellationToken cancellationToken = default)
            => await Get(expression, true, true, cancellationToken);
        public async Task<TEntityDTO> Get(Expression<Func<TEntityDTO, bool>> expression, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default)
            => await MapEntityToDto<TEntityDTO>(GetQueryable(loadNavigationProperties, readOnly).Where(ExpressionTransformer.Tranform<TEntityDTO, TEntity>(expression, Mapper)), cancellationToken);

        public async Task<TEntityDTO> Get(
            TKey id,
            bool loadNavigationProperties = true,
            bool readOnly = true,
            CancellationToken cancellationToken = default)
            => await MapEntityToDto<TEntityDTO>(GetQueryable(loadNavigationProperties, readOnly).Where(item => item.Id.Equals(id)), cancellationToken);

        public async Task<TEntityDTO> Get(TKey id, CancellationToken cancellationToken)
            => await Get(id, true, true, cancellationToken);

        public async Task<TDTO> Get<TDTO>(TKey id, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default) where TDTO : class, IDTO<TKey>
            => await MapEntityToDto<TDTO>(GetQueryable(loadNavigationProperties, readOnly).Where(item => item.Id.Equals(id)), cancellationToken);

        public async Task<IEnumerable<TEntityDTO>> GetBySite(int siteId, CancellationToken cancellationToken)
        {
            Filter filter = new Filter()
            {
                Filters = new List<FilterInfoBase>
                {
                    new NumberFilter
                    {
                        MatchMode = CountableFilterMatchMode.Equals,
                        PropertyName = "SiteId",
                        Value = siteId
                    }
                }
            };

            return await GetAll(filter: filter, cancellationToken: cancellationToken);
        }

        public async Task<IEnumerable<TEntityDTO>> GetAll(
            bool loadNavigationProperties = true,
            bool readOnly = true,
            CancellationToken cancellationToken = default)
            => await GetAll(null, loadNavigationProperties, readOnly, cancellationToken);

        public async Task<IEnumerable<TEntityDTO>> GetAll(CancellationToken cancellationToken)
            => await GetAll(true, true, cancellationToken);

        public async Task<IEnumerable<TEntityDTO>> GetAll(Filter filter, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default)
        {
            var query = GetQueryable(loadNavigationProperties, readOnly);
            if (filter != null)
            {
                query = ApplyFilter(query, filter);
            }
            return await MapEntitiesToDtos<TEntityDTO>(query);
        }

        public async Task<IEnumerable<TEntityDTO>> GetAll(Filter filter, CancellationToken cancellationToken = default)
            => await GetAll(filter, true, true, cancellationToken);

        public virtual IQueryable<TEntity> ApplyFilter(IQueryable<TEntity> query, Filter filter) => FilterSortService<TEntity, TKey>.ApplyFilter(query, filter);

        protected IQueryable<TEntity> GetQueryable(bool loadNavigationProperties, bool readOnly) =>
            readOnly ? GetQueryable(loadNavigationProperties).AsNoTracking() : GetQueryable(loadNavigationProperties);

        protected IQueryable<TEntity> InitialQuery { get; }

        protected virtual IQueryable<TEntity> ConfigureDataReader(IQueryable<TEntity> queryable) => queryable;

        private IQueryable<TEntity> GetQueryable() => ConfigureDataReader(UpdatedQuery ?? InitialQuery);

        private IQueryable<TEntity> UpdatedQuery { get; set; }

        private IQueryable<TEntity> GetQueryable(bool loadNavigationProperties) =>
            loadNavigationProperties ? GetQueryable() : InitialQuery;

        IQueryable<TEntity> IDataReaderConfigurator<TEntity, TKey>.GetQueryable()
        {
            return InitialQuery;
        }

        void IDataReaderConfigurator<TEntity, TKey>.SetQueryable(IQueryable<TEntity> entities)
        {
            UpdatedQuery = entities;
        }

        private async Task<TDTO> MapEntityToDto<TDTO>(IQueryable<TEntity> entity, CancellationToken cancellationToken = default) where TDTO : IDTO<TKey>
        {
            if (UseMappingOnDb)
            {
                return await entity
                    .ProjectTo<TDTO>(Mapper.ConfigurationProvider)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            return Mapper.Map<TDTO>(await entity.FirstOrDefaultAsync(cancellationToken));
        }

        protected async Task<IEnumerable<TDTO>> MapEntitiesToDtos<TDTO>(IQueryable<TEntity> entities, CancellationToken cancellationToken = default) where TDTO : IDTO<TKey>
        {
            if (UseMappingOnDb)
            {
                return await entities
                    .ProjectTo<TDTO>(Mapper.ConfigurationProvider)
                    .ToListAsync(cancellationToken);
            }
            return Mapper.Map<IEnumerable<TDTO>>(await entities.ToListAsync(cancellationToken));
        }

        
    }
}