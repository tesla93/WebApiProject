using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Core.Data
{
    public interface IDbContextModuleLinkage
    {
        IServiceCollection AddDbContext(DatabaseConnectionSettings connectionSettings,
            IConfiguration configuration, IServiceCollection services);

        Type GetPrivateDbContextType() => null;
    }
}
