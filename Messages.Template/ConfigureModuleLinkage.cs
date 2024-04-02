using ModuleLinkage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Core.ModelHashing;
using Messages.Templates;

namespace Core
{
    public class ConfigureModuleLinkage: IConfigureModuleLinkage
    {
        public void ConfigureModule(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var modelHashingService = serviceScope.ServiceProvider.GetService<IModelHashingService>();
                // Models hashing settings
                modelHashingService.ManualPropertyHashing<EmailDTO, EmailTemplate>(e => e.EmailTemplateId);
            }
        }
    }
}