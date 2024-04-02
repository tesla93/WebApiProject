using AutoMapper;
using Core.Crud.Interfaces;
using Core.Data;
using Core.DTO;
using System;

namespace Core.Crud
{
    public class SafePagedCrudService<TEntity, TEntityDTO, TListEntityDTO, TKey> : PagedCrudService<TEntity, TEntityDTO, TListEntityDTO, TKey>
        where TEntity : class, IEntity<TKey>, ISafeRemovable
        where TEntityDTO : class, IDTO<TKey>
        where TListEntityDTO : class, IDTO<TKey>
        where TKey : IEquatable<TKey>
    {
        public SafePagedCrudService(IDbContext context, IMapper mapper, bool useMappingOnQueries = false) : base
            (
                context,
                mapper,
                new PagedDataReader<TEntity, TEntityDTO, TListEntityDTO, TKey>(context, mapper, useMappingOnQueries),
                new DataWriter<TEntity, TEntityDTO, TKey>(context, mapper),
                new DataSafeRemover<TEntity, TKey>(context)
            )
        {
        }
    }

    public class SafePagedCrudService<TEntity, TEntityDTO, TKey> : SafePagedCrudService<TEntity, TEntityDTO, TEntityDTO, TKey>, IPagedCrudService<TEntityDTO, TKey>
        where TEntity : class, IEntity<TKey>, ISafeRemovable
        where TEntityDTO : class, IDTO<TKey>
        where TKey : IEquatable<TKey>
    {
        public SafePagedCrudService(IDbContext context, IMapper mapper, bool useMappingOnQueries = false) : base(context, mapper, useMappingOnQueries)
        {
        }
    }

    public class SafePagedCrudService<TEntity, TEntityDTO> : PagedCrudService<TEntity, TEntityDTO, int>, IPagedCrudService<TEntityDTO, int>, IPagedCrudService<TEntityDTO>
        where TEntity : class, IEntity, ISafeRemovable
        where TEntityDTO : class, IDTO
    {
        public SafePagedCrudService(IDbContext context, IMapper mapper, bool useMappingOnQueries = false) : base(context, mapper, useMappingOnQueries)
        {
        }
    }
}