using Core.Data;
using Core.DTO;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Crud.Interfaces
{
    public interface IDataWriter<TEntityDTO, TKey>
        where TEntityDTO : class, IDTO<TKey>
        where TKey : IEquatable<TKey>
    {
        Task<TEntityDTO> Save(TEntityDTO dto, bool saveChanges = true, CancellationToken cancellationToken = default);
        Task<TEntityDTO> Save(TEntityDTO dto, CancellationToken cancellationToken);
    }
}