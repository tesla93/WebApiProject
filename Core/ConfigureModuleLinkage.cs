using ModuleLinkage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using Core.ModelHashing;
using Core.Data;

namespace Core
{
    public class ConfigureModuleLinkage: IConfigureModuleLinkage
    {
        public void ConfigureModule(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var mapper = serviceScope.ServiceProvider.GetService<IMapper>();
                var context = serviceScope.ServiceProvider.GetService<IDbContext>();
                var modelHashingService = serviceScope.ServiceProvider.GetService<IModelHashingService>();
                modelHashingService.Register(mapper, context);
            }
        }
    }
}