using Core.DTO;
using Core.Filters;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Crud.Interfaces
{
    public interface IPagedDataReader<TEntityDTO, TListEntityDTO, TKey>: IDataReader<TEntityDTO, TKey>
        where TEntityDTO : class, IDTO<TKey>
        where TListEntityDTO : class, IDTO<TKey>
        where TKey : IEquatable<TKey>
    {
        Task<PageResult<TListEntityDTO>> GetPage(FilterCommand command = null, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default);
        Task<PageResult<TListEntityDTO>> GetPage(FilterCommand command, CancellationToken cancellationToken);
        Task<PageResult<TListEntityDTO>> GetPage(CancellationToken cancellationToken);
        Task<PageResult<TDTO>> GetPage<TDTO>(FilterCommand command = null, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default) where TDTO : class, IDTO<TKey>;
    }

    public interface IPagedDataReader<TEntityDTO, TKey> : IPagedDataReader<TEntityDTO, TEntityDTO, TKey>
        where TEntityDTO : class, IDTO<TKey>
        where TKey : IEquatable<TKey>
    {
    }

    public interface IPagedDataReader<TEntityDTO> : IPagedDataReader<TEntityDTO, int>
        where TEntityDTO : class, IDTO
    {
    }
}