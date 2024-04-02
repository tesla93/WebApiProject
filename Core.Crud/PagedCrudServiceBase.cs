using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Crud.Interfaces;
using Core.Data;
using Core.DTO;
using Core.Filters;
using Core.Model;
using Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Core.Crud
{
    /// <summary>
    /// Should be used with data managers like UserManager or RoleManager
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TEntityDTO"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public abstract class PagedCrudServiceBase<TEntity, TEntityDTO, TListEntityDTO, TKey> : DataService, IPagedCrudService<TEntityDTO, TListEntityDTO, TKey>
        where TEntityDTO : class, IDTO<TKey>
        where TListEntityDTO : class, IDTO<TKey>
        where TEntity : class, IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        public IMapper Mapper { get; }

        protected PagedCrudServiceBase(IDbContext context, IMapper mapper) : base(context)
        {
            Mapper = mapper;
        }

        public virtual bool UseMappingOnDb { get; set; }

        public abstract Task<TEntityDTO> Save(TEntityDTO dto, bool saveChanges, CancellationToken cancellationToken);

        public async Task<TEntityDTO> Save(TEntityDTO dto, CancellationToken cancellationToken) => await Save(dto, true, cancellationToken);

        public abstract Task Delete(TKey id, CancellationToken cancellationToken = default);

        public Task DeleteAll(CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public async Task<PageResult<TListEntityDTO>> GetPage(FilterCommand command = null, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default)
        {
            var dataPage = await GetDataPage(command, loadNavigationProperties, readOnly, cancellationToken);
            return new PageResult<TListEntityDTO>
            {
                Items = Mapper.Map<IEnumerable<TListEntityDTO>>(dataPage.Items),
                Total = dataPage.Total
            };
        }

        public async Task<PageResult<TListEntityDTO>> GetPage(FilterCommand command, CancellationToken cancellationToken) => await GetPage(command, true, true, cancellationToken);

        public async Task<PageResult<TListEntityDTO>> GetPage(CancellationToken cancellationToken) => await GetPage(null, true, true, cancellationToken);

        public async Task<PageResult<TDTO>> GetPage<TDTO>(FilterCommand command = null, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default) where TDTO : class, IDTO<TKey>
        {
            var dataPage = await GetDataPage(command, loadNavigationProperties, readOnly, cancellationToken);
            return new PageResult<TDTO>
            {
                Items = Mapper.Map<IEnumerable<TDTO>>(dataPage.Items),
                Total = dataPage.Total
            };
        }

        public async Task<TEntityDTO> Get(Expression<Func<TEntityDTO, bool>> expression, CancellationToken cancellationToken = default) =>
            await Get(expression, true, true);

        public async Task<TEntityDTO> Get(Expression<Func<TEntityDTO, bool>> expression, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default) =>
            Mapper.Map<TEntityDTO>(await GetQueryable(loadNavigationProperties, readOnly).FirstOrDefaultAsync(ExpressionTransformer.Tranform<TEntityDTO, TEntity>(expression, Mapper), cancellationToken));


        public async Task<TEntityDTO> Get(TKey id, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default) =>
            Mapper.Map<TEntityDTO>(await GetQueryable(loadNavigationProperties, readOnly).FirstOrDefaultAsync(x => x.Id.Equals(id), cancellationToken));

        public async Task<TEntityDTO> Get(TKey id, CancellationToken cancellationToken) =>
            await Get(id, true, true, cancellationToken);

        public async Task<TDTO> Get<TDTO>(TKey id, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default) where TDTO : class, IDTO<TKey> =>
            Mapper.Map<TDTO>(await GetQueryable(loadNavigationProperties, readOnly).FirstOrDefaultAsync(x => x.Id.Equals(id), cancellationToken));

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

        public async Task<NavigationalIdDTO<TEntityDTO, TKey>> GetEntitiesForNavigation([FromQuery] FilterCommand filterCommand, TKey id, CancellationToken cancellationToken)
        {
            var dbSet = GetQueryable(true, true);
            if (filterCommand != null)
            {
                dbSet = FilterSortService<TEntity, TKey>.ApplyFilter(dbSet, filterCommand);
                dbSet = FilterSortService<TEntity, TKey>.ApplySorting(dbSet, filterCommand);
            }
            TEntity previous = null;
            TEntity next = null;
            var sortField = filterCommand.SortField;

            var sortPropertyInfo = typeof(TEntity).GetProperty(sortField, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (sortPropertyInfo == null)
            {
                sortField = "Id";
            }
            var current = await dbSet.FirstOrDefaultAsync(entityItem => entityItem.Id.Equals(id));
            var currentPropertyValue = current.GetType().GetProperty(sortField).GetValue(current);
            if (typeof(TEntity).GetProperty(sortField).PropertyType == typeof(string))
            {
                if (filterCommand.SortOrder == OrderDirection.Asc)
                {
                    next = await dbSet.WhereDynamic($"x=> x.{sortField}.CompareTo(\"{currentPropertyValue}\") > 0").FirstOrDefaultAsync()
                           ?? await dbSet.FirstAsync();
                    previous = await dbSet.OrderByDescendingDynamic($"x=>x.{sortField}").WhereDynamic($"x=> x.{sortField}.CompareTo(\"{currentPropertyValue}\") < 0").FirstOrDefaultAsync()
                               ?? await dbSet.OrderByDescendingDynamic($"x=>x.{sortField}").FirstAsync();
                }
                else
                {
                    next = await dbSet.WhereDynamic($"x=> x.{sortField}.CompareTo(\"{currentPropertyValue}\") < 0").FirstOrDefaultAsync()
                           ?? await dbSet.FirstAsync();
                    previous = await dbSet.OrderByDynamic($"x=>x.{sortField}").WhereDynamic($"x=> x.{sortField}.CompareTo(\"{currentPropertyValue}\") > 0").FirstOrDefaultAsync()
                               ?? await dbSet.OrderByDynamic($"x=>x.{sortField}").FirstAsync();
                }
            }
            else
            {
                if (filterCommand.SortOrder == OrderDirection.Asc)
                {
                    next = await dbSet.WhereDynamic($"x=>x.{sortField} > {currentPropertyValue}").FirstOrDefaultAsync()
                                 ?? await dbSet.FirstAsync();
                    previous = await dbSet.OrderByDescendingDynamic($"x=>x.{sortField}").WhereDynamic($"x=>x.{sortField} < {currentPropertyValue}").FirstOrDefaultAsync()
                               ?? await dbSet.OrderByDescendingDynamic($"x=>x.{sortField}").FirstAsync();
                }
                else
                {
                    next = await dbSet.WhereDynamic($"x=>x.{sortField} < {currentPropertyValue}").FirstOrDefaultAsync()
                           ?? await dbSet.FirstAsync();
                    previous = await dbSet.OrderByDynamic($"x=>x.{sortField}").WhereDynamic($"x=>x.{sortField} > {currentPropertyValue}").FirstOrDefaultAsync()
                               ?? await dbSet.OrderByDynamic($"x=>x.{sortField}").FirstAsync();
                }
            }

            return new NavigationalIdDTO<TEntityDTO, TKey>() { Next = Mapper.Map<TEntityDTO>(next), Previous = Mapper.Map<TEntityDTO>(previous) };
        }

        public async Task<IEnumerable<TEntityDTO>> GetAll(CancellationToken cancellationToken) => await GetAll(true, true, cancellationToken);

        public async Task<IEnumerable<TEntityDTO>> GetAll(bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default) =>
            Mapper.Map<IEnumerable<TEntityDTO>>(await GetQueryable(loadNavigationProperties, readOnly).ToListAsync(cancellationToken));

        public async Task<IEnumerable<TEntityDTO>> GetAll(Filter filter, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default)
        {
            var query = GetQueryable(loadNavigationProperties, readOnly);
            if (filter != null)
            {
                ApplyFilter(query, filter);
            }
            return Mapper.Map<IEnumerable<TEntityDTO>>(await query.ToListAsync(cancellationToken));
        }

        public async Task<IEnumerable<TEntityDTO>> GetAll(Filter filter, CancellationToken cancellationToken = default) =>
            await GetAll(filter, true, true, cancellationToken);
        public abstract Task<bool> Exists(TKey id, CancellationToken cancellationToken = default);
        public async Task<bool> Exists(Expression<Func<TEntityDTO, bool>> expression, CancellationToken cancellationToken = default) =>
            await GetQueryable().AnyAsync(ExpressionTransformer.Tranform<TEntityDTO, TEntity>(expression, Mapper), cancellationToken);
        protected abstract IQueryable<TEntity> GetQueryable();
        protected virtual IQueryable<TEntity> ConfigurePagedDataReader(IQueryable<TEntity> entities) => ConfigureDataReader(entities);
        protected virtual IQueryable<TEntity> ConfigureDataReader(IQueryable<TEntity> entities) => entities;
        private IQueryable<TEntity> GetQueryable(bool loadNavigationProperties) =>
                loadNavigationProperties ? ConfigureDataReader(GetQueryable()) : GetQueryable();
        private IQueryable<TEntity> GetQueryable(bool loadNavigationProperties, bool readOnly) =>
            readOnly ? GetQueryable(loadNavigationProperties).AsNoTracking() : GetQueryable(loadNavigationProperties);

        private IQueryable<TEntity> GetPagedQueryable(bool loadNavigationProperties) =>
                loadNavigationProperties ? ConfigurePagedDataReader(GetQueryable()) : GetQueryable();
        private IQueryable<TEntity> GetPagedQueryable(bool loadNavigationProperties, bool readOnly) =>
            readOnly ? GetPagedQueryable(loadNavigationProperties).AsNoTracking() : GetPagedQueryable(loadNavigationProperties);

        public async Task<PageResult<TEntity>> GetDataPage(FilterCommand command = null, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default)
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
                    if (total % command.Take == 0 && total != 0)
                    {
                        maxSkip -= command.Take;
                    }
                    query = query.Skip(Math.Min(command.Skip, maxSkip)).Take(command.Take);
                }
            }
            else
            {
                total = await query.CountAsync(cancellationToken);
            }

            return new PageResult<TEntity>
            {
                Items = await query.ToListAsync(cancellationToken),
                Total = total
            };
        }

        protected virtual IQueryable<TEntity> ApplyFilter(IQueryable<TEntity> query, Filter filter) => FilterSortService<TEntity, TKey>.ApplyFilter(query, filter);
        protected virtual IQueryable<TEntity> ApplySorting(IQueryable<TEntity> query, ISorter command) => FilterSortService<TEntity, TKey>.ApplySorting(query, command);

        
    }
    public abstract class PagedCrudServiceBase<TEntity, TEntityDTO, TKey> : PagedCrudServiceBase<TEntity, TEntityDTO, TEntityDTO, TKey>
        where TEntityDTO : class, IDTO<TKey>
        where TEntity : class, IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        protected PagedCrudServiceBase(IDbContext context, IMapper mapper) : base(context, mapper)
        {
        }
    }
}