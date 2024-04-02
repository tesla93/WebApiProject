using Core.DTO;
using Core.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Crud.Interfaces
{
    public interface ICrudService<TEntityDTO, TKey> : IDataReader<TEntityDTO, TKey>, IDataWriter<TEntityDTO, TKey>, IDataRemover<TKey>, IDataService
        where TEntityDTO : class, IDTO<TKey>
        where TKey : IEquatable<TKey>
    {
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }

    public interface ICrudService<TEntityDTO> : ICrudService<TEntityDTO, int>
        where TEntityDTO : class, IDTO<int>, IDTO
    {
    }
}