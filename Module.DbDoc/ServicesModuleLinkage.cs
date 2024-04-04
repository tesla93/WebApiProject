using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Module.AutofacExtensions;
using Module.DbDoc.Services;
using Module.ModuleLinkage;
using Module.Core.Exceptions;
using Module.DbDoc.Core;

namespace Module.DbDoc
{
    public class ServicesModuleLinkage: IServicesModuleLinkage, IDependenciesModuleLinkage
    {
        private const string dbDocSection = "DBDocSettings";

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var section = configuration.GetSection(dbDocSection);
            if (section.Get<DBDocSettings>() == null)
                throw new EmptyConfigurationSectionException(dbDocSection);
            services.Configure<DBDocSettings>(section);
            services.AddSingleton<IDbContextProvider, DbContextProvider>();
        }

        public void RegisterDependencies(ContainerBuilder builder)
        {
            builder.RegisterService<IDbDocToolService, DbDocToolService>();
            builder.RegisterService<IDbContextScanner, DbContextScanner>();
            builder.RegisterService<IDatabaseScannerProvider, DatabaseScannerProvider>();
            builder.RegisterService<IDbMetadataService, DbMetadataService>();
            builder.RegisterService<IDbModelValidator, DbModelValidator>();
        }
    }
}