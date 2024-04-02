using System.Threading;
using System.Threading.Tasks;

namespace Core.Crud.Interfaces
{
    public interface IDataRemover<TKey>
    {
        Task Delete(TKey id, CancellationToken cancellationToken = default);
        Task DeleteAll(CancellationToken cancellationToken = default);
    }
}