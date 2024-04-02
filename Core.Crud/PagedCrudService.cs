using AutoMapper;
using Core.Crud.Interfaces;
using Core.Data;
using Core.DTO;
using Core.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Crud
{
    public class PagedCrudService<TEntity, TEntityDTO, TListEntityDTO, TKey> : CrudService<TEntity, TEntityDTO, TKey>, IPagedCrudService<TEntityDTO, TListEntityDTO, TKey>
        where TEntity : class, IEntity<TKey>
        where TListEntityDTO : class, IDTO<TKey>
        where TEntityDTO : class, IDTO<TKey>
        where TKey : IEquatable<TKey>
    {
        private IPagedDataReader<TEntityDTO, TListEntityDTO, TKey> PagedDataReader { get; }

        public PagedCrudService(IDbContext context, IMapper mapper, bool useMappingOnQueries = false) : this
            (
                context,
                mapper,
                new PagedDataReader<TEntity, TEntityDTO, TListEntityDTO, TKey>(context, mapper, useMappingOnQueries),
                new DataWriter<TEntity, TEntityDTO, TKey>(context, mapper),
                new DataRemover<TEntity, TKey>(context)
            )
        {
        }

        public PagedCrudService(IDbContext context, IMapper mapper, IPagedDataReader<TEntityDTO, TListEntityDTO, TKey> pagedDataReader) : this
            (
                context,
                mapper,
                pagedDataReader,
                new DataWriter<TEntity, TEntityDTO, TKey>(context, mapper),
                new DataRemover<TEntity, TKey>(context)
            )
        {
        }

        public PagedCrudService(IDbContext context, IMapper mapper, IPagedDataReader<TEntityDTO, TListEntityDTO, TKey> pagedDataReader, IDataWriter<TEntityDTO, TKey> dataWriter, IDataRemover<TKey> dataRemover)
            : base(context, mapper, pagedDataReader, dataWriter, dataRemover)
        {
            PagedDataReader = pagedDataReader;
        }

        public async Task<PageResult<TDTO>> GetPage<TDTO>(
            FilterCommand command = null,
            bool loadNavigationProperties = true,
            bool readOnly = true,
            CancellationToken cancellationToken = default) where TDTO: class, IDTO<TKey>
        {
            ConfigureDataReaderInternal(command);
            var result = await PagedDataReader.GetPage<TDTO>(command, loadNavigationProperties, readOnly, cancellationToken);
            return new PageResult<TDTO>
            {
                Total = result.Total,
                Items = Mapper.Map<IEnumerable<TDTO>>(result.Items),
                TempLogs = result.TempLogs
            };
        }

        public async Task<PageResult<TListEntityDTO>> GetPage(
            FilterCommand command = null,
            bool loadNavigationProperties = true,
            bool readOnly = true,
            CancellationToken cancellationToken = default)
        {
            ConfigureDataReaderInternal(command);
            return await PagedDataReader.GetPage(command, loadNavigationProperties, readOnly, cancellationToken);
        }

        public async Task<PageResult<TListEntityDTO>> GetPage(FilterCommand command, CancellationToken cancellationToken)
            => await GetPage(command, true, true, cancellationToken);

        public async Task<PageResult<TListEntityDTO>> GetPage(CancellationToken cancellationToken)
            => await GetPage(null, true, true, cancellationToken);

        protected virtual IQueryable<TEntity> ApplySorting(IQueryable<TEntity> query, ISorter command)
            => query;

        protected virtual IQueryable<TEntity> ConfigurePagedDataReader(IQueryable<TEntity> entities)
            => ConfigureDataReader(entities);

        protected async Task<PageResult<TDTO>> GetPage<TDTO>(Func<IQueryable<TEntity>, IQueryable<TEntity>> setup, FilterCommand command = null, CancellationToken cancellationToken = default) where TDTO : class, IDTO<TKey>
        {
            var query = GetConfigurator().GetQueryable();
            query = setup(query);
            query = ApplyFilter(query, command);
            query = ApplySorting(query, command);
            GetConfigurator().SetQueryable(query);
            return await PagedDataReader.GetPage<TDTO>(command, true, true, cancellationToken);
        }

        private void ConfigureDataReaderInternal(FilterCommand command)
        {
            var query = GetConfigurator().GetQueryable();
            query = ConfigurePagedDataReader(query);
            query = ApplyFilter(query, command);
            query = ApplySorting(query, command);
            GetConfigurator().SetQueryable(query);
        }

        private IPagedDataReaderConfigurator<TEntity, TKey> GetConfigurator()
        {
            return (IPagedDataReaderConfigurator<TEntity, TKey>)PagedDataReader;
        }
    }

    public class PagedCrudService<TEntity, TEntityDTO, TKey> : PagedCrudService<TEntity, TEntityDTO, TEntityDTO, TKey>, IPagedCrudService<TEntityDTO, TKey>
        where TEntity : class, IEntity<TKey>
        where TEntityDTO : class, IDTO<TKey>
        where TKey : IEquatable<TKey>
    {
        public PagedCrudService(IDbContext context, IMapper mapper, bool useMappingOnQueries = false) : base(context, mapper, useMappingOnQueries)
        {
        }
    }

    public class PagedCrudService<TEntity, TEntityDTO> : PagedCrudService<TEntity, TEntityDTO, int>, IPagedCrudService<TEntityDTO, int>, IPagedCrudService<TEntityDTO>
        where TEntity : class, IEntity
        where TEntityDTO : class, IDTO
    {
        public PagedCrudService(IDbContext context, IMapper mapper, bool useMappingOnQueries = false) : base(context, mapper, useMappingOnQueries)
        {
        }
    }
}