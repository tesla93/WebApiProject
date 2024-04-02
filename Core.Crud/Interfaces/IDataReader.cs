using Core.DTO;
using Core.Filters;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Crud.Interfaces
{
    public interface IDataReader<TEntityDTO, TKey>
        where TEntityDTO : class, IDTO<TKey>
        where TKey : IEquatable<TKey>
    {
        bool UseMappingOnDb { get; set; }
        Task<TEntityDTO> Get(Expression<Func<TEntityDTO, bool>> expression, CancellationToken cancellationToken = default);
        Task<TEntityDTO> Get(Expression<Func<TEntityDTO, bool>> expression, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default);
        Task<TEntityDTO> Get(TKey id, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default);
        Task<TEntityDTO> Get(TKey id, CancellationToken cancellationToken);
        Task<TDTO> Get<TDTO>(TKey id, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default) where TDTO : class, IDTO<TKey>;
        Task<IEnumerable<TEntityDTO>> GetAll(CancellationToken cancellationToken);
        Task<IEnumerable<TEntityDTO>> GetBySite(int siteId, CancellationToken cancellationToken);
        Task<IEnumerable<TEntityDTO>> GetAll(bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default);
        Task<IEnumerable<TEntityDTO>> GetAll(Filter filter, bool loadNavigationProperties = true, bool readOnly = true, CancellationToken cancellationToken = default);
        Task<IEnumerable<TEntityDTO>> GetAll(Filter filter, CancellationToken cancellationToken = default);
        Task<bool> Exists(TKey id, CancellationToken cancellationToken = default);
        Task<bool> Exists(Expression<Func<TEntityDTO, bool>> expression, CancellationToken cancellationToken = default);
    }
}