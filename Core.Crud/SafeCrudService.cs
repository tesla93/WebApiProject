using AutoMapper;
using Core.Crud.Interfaces;
using Core.Data;
using Core.DTO;
using System;

namespace Core.Crud
{
    public class SafeCrudService<TEntity, TEntityDTO, TKey> : CrudService<TEntity, TEntityDTO, TKey>
        where TEntity : class, IEntity<TKey>, ISafeRemovable
        where TEntityDTO : class, IDTO<TKey>
        where TKey : IEquatable<TKey>
    {
        public SafeCrudService(IDbContext context, IMapper mapper, bool useMappingOnQueries = false) : base
            (
                context,
                mapper,
                new DataReader<TEntity, TEntityDTO, TKey>(context, mapper, useMappingOnQueries),
                new DataWriter<TEntity, TEntityDTO, TKey>(context, mapper),
                new DataSafeRemover<TEntity, TKey>(context)
            )
        {
        }
    }

    public class SafeCrudService<TEntity, TEntityDTO> : SafeCrudService<TEntity, TEntityDTO, int>, ICrudService<TEntityDTO>
       where TEntity : class, IEntity, ISafeRemovable
       where TEntityDTO : class, IDTO<int>, IDTO
    {
        public SafeCrudService(IDbContext context, IMapper mapper, bool useMappingOnQueries = false) : base(context, mapper, useMappingOnQueries)
        {
        }
    }
}