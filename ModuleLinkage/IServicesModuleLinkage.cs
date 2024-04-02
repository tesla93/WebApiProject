using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace ModuleLinkage
{
    public interface IServicesModuleLinkage
    {
        void ConfigureServices(IServiceCollection services, IConfiguration configuration);
    }
}
