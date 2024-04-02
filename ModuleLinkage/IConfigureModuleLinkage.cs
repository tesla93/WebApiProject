using Microsoft.AspNetCore.Builder;

namespace ModuleLinkage
{
    public interface IConfigureModuleLinkage
    {
        void ConfigureModule(IApplicationBuilder app);
    }
}
