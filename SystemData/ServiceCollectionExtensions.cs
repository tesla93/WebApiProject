using Autofac;
using AutofacExtensions;

namespace SystemData
{
    public static class ServiceCollectionExtensions
    {
        public static void RegisterSystemDataService(this ContainerBuilder builder)
        {            
            builder.RegisterService<ISystemDataService, SystemDataService>();
        }
    }
}