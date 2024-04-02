using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Core.Membership;
using Core.Web.Filters;
using FileStorage;

using Module.SystemSettings;
using ClaimTypes = System.Security.Claims.ClaimTypes;
using ControllerBase = Core.Web.ControllerBase;

namespace Project.Server.Api
{
    [Produces("application/json")]
    [Route("api/system-configuration")]
    [ReadWriteAuthorize(ReadRoles = ReadWriteAuthorizeAttribute.Authenticated, WriteRoles = Roles.SuperAdminRole + "," + Roles.SystemAdminRole)]
    public class SystemConfigurationController : ControllerBase
    {
        private const string UploadLogoImageOperationName = "ProjectLogoImageUploading";
        private const string UploadLogoIconOperationName = "ProjectLogoIconUploading";

        private readonly ISettingsService _settingsService;
        private readonly IFileStorageService _fileStorageService;
        //private readonly IHubContext<MaintenanceHub> _maintenanceHubContext;


        public SystemConfigurationController(
            ISettingsService settingsService,
            ILogger<SystemConfigurationController> logger,
            IFileStorageService fileStorageService
            //IHubContext<MaintenanceHub> maintenanceHubContext
            ) : base(logger)
        {
            _settingsService = settingsService;
            _fileStorageService = fileStorageService;
            //_maintenanceHubContext = maintenanceHubContext;
        }


        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Load() =>
            Ok(await _settingsService.Load(
                    User.Identity.IsAuthenticated
                        ? null
                        : new[]
                        {
                            SettingsName.UserPasswordSettings,
                            SettingsName.RegistrationSettings,
                            SettingsName.ProjectSettings,
                            SettingsName.PwaSettings,
                            SettingsName.FacebookSsoSettings,
                            SettingsName.GoogleSsoSettings,
                            SettingsName.LinkedInSsoSettings
                        }
                )
            );

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] SettingsDTO[] config)
        {
            var newProjectSettings = config.SingleOrDefault(section => section.SectionName == "ProjectSettings");
            if (newProjectSettings != null)
            {
                // Removing old logo images
                await DeleteProjectSettingsLogoFiles(newProjectSettings.Value);
            }

            var result = await _settingsService.Save(config);

            if (newProjectSettings != null)
            {
                // Unbinding files from user and operation name to complete operation
                await CompleteLogoImagesUploading();
            }

            var newMaintenanceSettingsSection = result.SingleOrDefault(section => section.SectionName == "MaintenanceSettings");
            if (newMaintenanceSettingsSection != null)
            {
                //var newMaintenanceSettings =
                //    JsonConvert.DeserializeObject<MaintenanceSettings>(JsonConvert.SerializeObject(newMaintenanceSettingsSection.Value));
                //await _maintenanceHubContext.Clients.All.SendAsync("InfoUpdated", newMaintenanceSettings);
            }

            return Ok(result);
        }

        //[HttpGet]
        //[Route("maintenance-settings")]
        //[DoNotResetAuthCookie]
        //public IActionResult MaintenanceSettings() =>
        //    Ok(_settingsService.GetSettingsSection<MaintenanceSettings>());

        //[HttpPost]
        //[Route("loading-times-settings")]
        //public IActionResult SetLoadingTimeSettings(bool recordLoadingTime, CancellationToken cancellationToken)
        //{
        //    var loadingTimeSettings = _settingsService.GetSettingsSection<LoadingTimeSettings>();
        //    loadingTimeSettings.RecordLoadingTime = recordLoadingTime;
        //    _settingsService.SaveSettingsSection(loadingTimeSettings);
        //    return Ok();
        //}

        [HttpPost, Route("upload-logo-image")]
        [AllowedFormFileFormats(1000000, "image/png", "image/jpeg", "image/gif")]
        public async Task<IActionResult> UploadLoginLogoImage(IFormCollection formData, CancellationToken cancellationToken)
        {
            var files = Request.Form.Files;

            if (files == null) return BadRequest("There is no uploaded file.");
            if (files.Count != 1) return BadRequest("The count of uploaded files should be 1.");

            // Binding files to user and operation name for removing old files
            var additionalData =
                formData.Keys.ToDictionary<string, string, string>(key => key, key => formData[key]);
            additionalData.Add("max_size", "10000");
            additionalData.Add("thumbnail_size", "400");
            additionalData.Add("user_id", User.FindFirstValue(ClaimTypes.NameIdentifier));
            additionalData.Add("operation_name", UploadLogoImageOperationName);

            return Ok((await _fileStorageService.UploadFiles(files.ToArray(), additionalData, cancellationToken)).SuccessfullyUploadedFiles[0]);
        }

        [HttpPost, Route("upload-logo-icon")]
        [AllowedFormFileFormats(100000, "image/x-icon")]
        public async Task<IActionResult> UploadLogoIcon(IFormCollection formData, CancellationToken cancellationToken)
        {
            var files = Request.Form.Files;

            if (files == null) return BadRequest("There is no uploaded file.");
            if (files.Count != 1) return BadRequest("The count of uploaded files should be 1.");

            // Binding files to user and operation name for removing old files
            var additionalData =
                formData.Keys.ToDictionary<string, string, string>(key => key, key => formData[key]);
            additionalData.Add("max_size", "50");
            additionalData.Add("thumbnail_size", "50");
            additionalData.Add("user_id", User.FindFirstValue(ClaimTypes.NameIdentifier));
            additionalData.Add("operation_name", UploadLogoIconOperationName);

            return Ok((await _fileStorageService.UploadFiles(files.ToArray(), additionalData, cancellationToken)).SuccessfullyUploadedFiles[0]);
        }


        private async Task DeleteProjectSettingsLogoFiles(object newProjectSettingsJson, CancellationToken cancellationToken = default)
        {
            var oldProjectSettings = _settingsService.GetSettingsSection<ProjectSettings>();
            var newProjectSettings = JsonConvert.DeserializeObject<ProjectSettings>(JsonConvert.SerializeObject(newProjectSettingsJson));

            if (oldProjectSettings.LogoImageId != null &&
                newProjectSettings.LogoImageId != oldProjectSettings.LogoImageId)
                await _fileStorageService.DeleteFile((int)oldProjectSettings.LogoImageId, cancellationToken);

            if (oldProjectSettings.LogoIconId != null &&
                newProjectSettings.LogoIconId != oldProjectSettings.LogoIconId)
                await _fileStorageService.DeleteFile((int)oldProjectSettings.LogoIconId, cancellationToken);
        }

        private async Task CompleteLogoImagesUploading(CancellationToken cancellationToken = default)
        {
            await _fileStorageService.CompleteUsersFilesUploadingOperation(
                User.FindFirstValue(ClaimTypes.NameIdentifier), UploadLogoImageOperationName, cancellationToken);
            await _fileStorageService.CompleteUsersFilesUploadingOperation(
                User.FindFirstValue(ClaimTypes.NameIdentifier), UploadLogoIconOperationName, cancellationToken);
        }
    }
}