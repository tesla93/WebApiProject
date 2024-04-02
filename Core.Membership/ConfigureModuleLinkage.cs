using ModuleLinkage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Core.ModelHashing;
using Core.Membership.DTO;
using Core.Membership.Model;

namespace Core.Membership
{
    public class ConfigureModuleLinkage : IConfigureModuleLinkage
    {
        public void ConfigureModule(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var modelHashingService = serviceScope.ServiceProvider.GetService<IModelHashingService>();
                // Models hashing settings
                modelHashingService.IgnoreModelHashing<LoginAuditDTO>();
                modelHashingService.IgnorePropertiesHashing<LoginAuditDTO>(a => a.Id);
                modelHashingService.ManualPropertyHashing<CompanyDTO, Company>(e => e.BrandingId);
                modelHashingService.ManualPropertyHashing<CompanyDTO, Company>(c => c.Id);
                modelHashingService.ManualPropertyHashing<BrandingDTO, Company>(b => b.Id);
            }
        }
    }
}