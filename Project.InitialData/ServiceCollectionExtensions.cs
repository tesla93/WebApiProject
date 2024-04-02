using Autofac;
using Microsoft.Extensions.DependencyInjection;
using AutofacExtensions;

namespace InitialData
{
    public static class ServiceCollectionExtensions
    {
        public static void RegisterInitialDataServices(this ContainerBuilder builder)
        {
            builder.RegisterService<IDatabaseInitializerService, DatabaseInitializerService>();
        }

        public static void EnsureInitialProjectData(this IServiceScope serviceScope)
        {
            var databaseInitializerService = serviceScope.ServiceProvider.GetService<IDatabaseInitializerService>();
            databaseInitializerService.EnsureInitialData();
        }

        

        public static void InitRouteRoles(this IServiceScope serviceScope)
        {
            RouteRolesDataService.InitRouteRoles(serviceScope);
        }
    }
}