using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace ModuleLinkage
{
    public interface IDbMigrateModuleLinkage
    {
        Task Migrate(IServiceScope serviceScope);
    }
}
