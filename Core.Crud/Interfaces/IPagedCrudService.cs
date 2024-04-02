using Core.Crud.Interfaces;
using Core.DTO;
using System;

namespace Core.Crud.Interfaces
{
    public interface IPagedCrudService<TEntityDTO, TListEntityDTO, TKey> : ICrudService<TEntityDTO, TKey>, IPagedDataReader<TEntityDTO, TListEntityDTO, TKey>
        where TEntityDTO : class, IDTO<TKey>
        where TListEntityDTO : class, IDTO<TKey>
        where TKey : IEquatable<TKey>
    {
    }

    public interface IPagedCrudService<TEntityDTO, TKey> : IPagedCrudService<TEntityDTO, TEntityDTO, TKey>, ICrudService<TEntityDTO, TKey>, IPagedDataReader<TEntityDTO, TEntityDTO, TKey>
        where TEntityDTO : class, IDTO<TKey>
        where TKey : IEquatable<TKey>
    {
    }

    public interface IPagedCrudService<TEntityDTO> : IPagedCrudService<TEntityDTO, int>, ICrudService<TEntityDTO>, IPagedDataReader<TEntityDTO, int>, IPagedDataReader<TEntityDTO>
        where TEntityDTO : class, IDTO
    {
    }
}