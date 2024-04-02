using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AppConfiguration
{
    public class FakeAppConfigurationService : IAppConfigurationService
    {
        public Task Delete(string name, CancellationToken cancellationToken = default) => null;

        public async Task<IEnumerable<Parameter>> GetAll(CancellationToken cancellationToken = default) => new List<Parameter>();

        public Task<Parameter> GetByName(string name, CancellationToken cancellationToken = default) => null; 

        public Task Put(Parameter parameter, CancellationToken cancellationToken = default) => null;
    }
}