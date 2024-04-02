using Autofac;
using ModuleLinkage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BackgroundProcessing
{
    public class ServicesModuleLinkage : IServicesModuleLinkage
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddHostedService<QueuedHostedService>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
        }
    }
}