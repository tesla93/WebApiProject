using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AppConfiguration
{
    public interface IAppConfigurationService
    {
        Task<IEnumerable<Parameter>> GetAll(CancellationToken cancellationToken = default);

        Task<Parameter> GetByName(string name, CancellationToken cancellationToken = default);

        Task Put(Parameter parameter, CancellationToken cancellationToken = default);

        Task Delete(string name, CancellationToken cancellationToken = default);
    }
}
