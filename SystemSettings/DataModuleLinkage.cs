using ModuleLinkage;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Module.SystemSettings
{
    public class DataModuleLinkage: IDataModuleLinkage
    {
        public Task EnsureInitialData(IServiceScope serviceScope)
        {
            var service = serviceScope.ServiceProvider.GetService<ISettingsService>();

            var appSettings = new[]
            {
                new SettingsDTO { Value = service.GetSettingsSection<ProjectSettings>() ?? new ProjectSettings() },
                new SettingsDTO { Value = service.GetSettingsSection<PwaSettings>() ?? new PwaSettings() }
            };

            return service.Save(appSettings);
        }
    }
}