using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Core.Membership;
using ControllerBase = Core.Web.ControllerBase;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace AppConfiguration
{
    [Produces("application/json")]
    [Route("api/app-settings")]
    [Authorize(Roles = Roles.SystemAdminRole + "," + Roles.SuperAdminRole)]
    public class AppConfigurationController : ControllerBase
    {
        private readonly IAppConfigurationService _appSettingsService;
        private readonly bool _isParametersStoreEnabled;


        public AppConfigurationController(
            ILogger<AppConfigurationController> logger,
            IAppConfigurationService appSettingsService,
            IConfiguration configuration) : base(logger)
        {
            _appSettingsService = appSettingsService;

            var configurationProviderName = configuration.GetSection("StorageSettings")?.GetValue<string>("ProviderName");
            _isParametersStoreEnabled = configurationProviderName != null && (configurationProviderName == "AWS" || configurationProviderName == "Azure");
        }


        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default) =>
            Ok(await _appSettingsService.GetAll(cancellationToken));

        [HttpGet]
        [Route("{name}")]
        public async Task<IActionResult> GetByName(string name, CancellationToken cancellationToken = default) =>
            Ok(await _appSettingsService.GetByName(name, cancellationToken));

        [HttpGet]
        [Route("is-enabled")]
        public IActionResult IsEnabled() => Ok(_isParametersStoreEnabled);

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] Parameter dto, CancellationToken cancellationToken = default)
            => await NoContent(() => _appSettingsService.Put(dto, cancellationToken));

        [HttpDelete]
        [Route("{name}")]
        public async Task<IActionResult> Delete(string name, CancellationToken cancellationToken = default)
            => await NoContent(() => _appSettingsService.Delete(name, cancellationToken));
    }
}
