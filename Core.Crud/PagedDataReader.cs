using AutoMapper;
using Core.Crud.Interfaces;
using Core.Data;
using Core.DTO;
using Core.Filters;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Crud
{
    public class PagedDataReader<TEntity, TEntityDTO, TListEntityDTO, TKey> :
        DataReader<TEntity, TEntityDTO, TKey>,
        IPagedDataReader<TEntityDTO, TListEntityDTO, TKey>,
        IDataReaderConfigurator<TEntity, TKey>,
        IPagedDataReaderConfigurator<TEntity, TKey>
        where TEntity : class, IEntity<TKey>
        where TEntityDTO : class, IDTO<TKey>
        where TListEntityDTO : class, IDTO<TKey>
        where TKey : IEquatable<TKey>
    {
        public PagedDataReader(IDbContext dbContext, IMapper mapper, bool useMappingOnDb = false) : base(dbContext, mapper, useMappingOnDb)
        {
        }

        public async Task<PageResult<TDTO>> GetPage<TDTO>(FilterCommand command = null, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default) where TDTO : class, IDTO<TKey>
            => await GetDataPage<TDTO>(command, loadNavigationProperties, readOnly, cancellationToken);

        public async Task<PageResult<TListEntityDTO>> GetPage(FilterCommand command = null, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default)
            => await GetPage<TListEntityDTO>(command, loadNavigationProperties, readOnly, cancellationToken);
        public async Task<PageResult<TListEntityDTO>> GetPage(FilterCommand command, CancellationToken cancellationToken)
            => await GetPage(command, true, true, cancellationToken);

        public async Task<PageResult<TListEntityDTO>> GetPage(CancellationToken cancellationToken)
            => await GetPage(null, true, true, cancellationToken);

        public virtual IQueryable<TEntity> ApplySorting(IQueryable<TEntity> query, FilterCommand command)
            => FilterSortService<TEntity, TKey>.ApplySorting(query, command);

        protected virtual IQueryable<TEntity> ConfigurePagedDataReader(IQueryable<TEntity> queryable)
            => queryable;

        protected IQueryable<TEntity> GetPagedQueryable(bool loadNavigationProperties, bool readOnly)
            => readOnly ? GetPagedQueryable(loadNavigationProperties).AsNoTracking() : GetPagedQueryable(loadNavigationProperties);

        private IQueryable<TEntity> GetPagedQueryable(bool loadNavigationProperties)
            => loadNavigationProperties ? GetPagedQueryable() : InitialQuery;
        private IQueryable<TEntity> GetPagedQueryable()
            => ConfigurePagedDataReader(UpdatedQuery ?? InitialQuery);

        IQueryable<TEntity> IPagedDataReaderConfigurator<TEntity, TKey>.GetQueryable()
        {
            return InitialQuery;
        }

        void IPagedDataReaderConfigurator<TEntity, TKey>.SetQueryable(IQueryable<TEntity> entities)
        {
            UpdatedQuery = entities;
        }

        private IQueryable<TEntity> UpdatedQuery { get; set; }

        private async Task<PageResult<TDTO>> GetDataPage<TDTO>(FilterCommand command = null, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default) where TDTO: class, IDTO<TKey>
        {
            var query = GetPagedQueryable(loadNavigationProperties, readOnly);
            int total;

            if (command != null)
            {
                query = ApplyFilter(query, command);
                query = ApplySorting(query, command);

                total = await query.CountAsync(cancellationToken);

                if (command.IsPaginator)
                {
                    var maxSkip = (total / command.Take) * command.Take;
                    if (total % command.Take == 0 && total != 0) maxSkip -= command.Take;

                    query = query.Skip(Math.Min(command.Skip, maxSkip)).Take(command.Take);
                }
            }
            else
            {
                total = await query.CountAsync(cancellationToken);
            }

            return new PageResult<TDTO>
            {
                Items = await MapEntitiesToDtos<TDTO>(query, cancellationToken),
                Total = total
            };
        }
    }

    public class PagedDataReader<TEntity, TEntityDTO, TKey> : PagedDataReader<TEntity, TEntityDTO, TEntityDTO, TKey>, IPagedDataReader<TEntityDTO, TKey>
        where TEntity : class, IEntity<TKey>
        where TEntityDTO : class, IDTO<TKey>
        where TKey : IEquatable<TKey>
    {
        public PagedDataReader(IDbContext context, IMapper mapper, bool useMappingOnDb = false) : base(context, mapper, useMappingOnDb)
        {
        }
    }

    public class PagedDataReader<TEntity, TEntityDTO> : PagedDataReader<TEntity, TEntityDTO, TEntityDTO, int>, IPagedDataReader<TEntityDTO>
        where TEntity : class, IEntity
        where TEntityDTO : class, IDTO
    {
        public PagedDataReader(IDbContext context, IMapper mapper, bool useMappingOnDb = false) : base(context, mapper, useMappingOnDb)
        {
        }
    }
}