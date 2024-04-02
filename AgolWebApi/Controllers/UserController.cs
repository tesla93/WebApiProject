using Core.Crud;
using Core.Exceptions;
using Core.Membership;
using Core.Membership.DTO;
using Core.Membership.Enums;
using Core.Membership.Services;
using Core.Web.Filters;
using FileStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ClaimTypes = System.Security.Claims.ClaimTypes;

namespace Server.Api
{
    [Produces("application/json")]
    [Route("api/user")]
    [ReadWriteAuthorize(ReadWriteRoles = Roles.SystemAdminRole)]
    public class UserController : PagedCrudControllerBase<UserDTO, string>
    {
        private const string UploadAvatarImageOperationName = "UserAvatarImageUploading";
        private readonly IFileStorageService _fileStorageService;

        private IUserService UserService => CrudService as IUserService;

        public UserController(
            IUserService service,
            ILogger<UserController> logger,
            IFileStorageService fileStorageService) : base(service, logger)
        {
            _fileStorageService = fileStorageService;
        }


        [ResponseCache(NoStore = true)]
        public override async Task<IActionResult> Get(string id, CancellationToken cancellationToken = default) =>
            await base.Get(id, cancellationToken);

        public override async Task<IActionResult> Update(UserDTO dto, string id, CancellationToken cancellationToken = default)
        {
            var existingUser = await UserService.Get(dto.Id, cancellationToken);

            if (existingUser == null)
                throw new EntityNotFoundException("User not found.");

            var userByEmail = await UserService.GetByEmail(dto.Email, false, cancellationToken);
            if (userByEmail != null && userByEmail.Id != dto.Id)
                throw new InvalidModelException("User with specified email already exists.");

            dto.AccountStatus = existingUser.Email != dto.Email && existingUser.AccountStatus == AccountStatus.Active
                ? AccountStatus.Unverified
                : existingUser.AccountStatus;

            return Ok(await UserService.Save(dto, cancellationToken));
        }

        [HttpPost]
        [Route("invite")]
        public async Task<IActionResult> Invite([FromBody] UserDTO user, CancellationToken cancellationToken = default) =>
            Ok(await UserService.Invite(user, cancellationToken));

        [HttpPost]
        [Route("{userId}/resend-invitation")]
        public async Task<IActionResult> ResendInvitation(string userId, CancellationToken cancellationToken = default)
        {
            if (!await UserService.Exists(userId, cancellationToken))
                throw new EntityNotFoundException("User not found.");

            await UserService.ResendInvitation(userId, cancellationToken);
            return NoContent();
        }

        [HttpPost]
        [Route("{userId}/resend-email-confirmation")]
        public async Task<IActionResult> ResendEmailConfirmation(string userId, CancellationToken cancellationToken)
        {
            if (!await UserService.Exists(userId, cancellationToken))
                throw new EntityNotFoundException("User not found.");

            await UserService.ResendEmailConfirmation(userId, cancellationToken);
            return NoContent();
        }

        [Route("me")]
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetLoggedUser() =>
                Ok(User.Identity.IsAuthenticated
                    ? await UserService.Get(User.FindFirstValue(ClaimTypes.NameIdentifier))
                    : null);

        [HttpPost]
        [Route("me/{impersonatedUserId}/impersonate")]
        public async Task<IActionResult> ImpersonateCurrentUserAsUser(string impersonatedUserId, CancellationToken cancellationToken = default)
        {
            if (!await UserService.Exists(impersonatedUserId, cancellationToken))
                throw new EntityNotFoundException("Impersonated user not found.");

            return Ok(await UserService.Impersonate(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                impersonatedUserId,
                cancellationToken
            ));
        }

        [HttpPost]
        [Route("me/stop-impersonation")]
        [Authorize]
        public async Task<IActionResult> StopCurrentUserImpersonation() =>
            Ok(await UserService.StopImpersonation(User));

        [HttpGet]
        [Route("me/is-impersonating")]
        [Authorize]
        public async Task<IActionResult> IsCurrentUserImpersonating() =>
            Ok(await UserService.IsUserImpersonating(User));

        [Route("me/{impersonatedUserId}/can-impersonate")]
        [HttpGet]
        public async Task<IActionResult> CanCurrentUserImpersonateUser(string impersonatedUserId) =>
            Ok(await UserService.CanImpersonate(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                impersonatedUserId));

        [HttpPost, Route("{userId}/approve")]
        public async Task<IActionResult> Approve(string userId, CancellationToken cancellationToken)
        {
            if (!await UserService.Exists(userId, cancellationToken))
                throw new EntityNotFoundException("User not found.");

            await UserService.Approve(userId, cancellationToken);
            return NoContent();
        }

        [HttpPost, Route("{userId}/toggle-locking")]
        public async Task<IActionResult> ToggleLocking(string userId, CancellationToken cancellationToken)
        {
            if (!await UserService.Exists(userId, cancellationToken))
                throw new EntityNotFoundException("User not found.");

            await UserService.ToggleLocking(userId, cancellationToken);
            return NoContent();
        }

        [HttpPost, Route("{userId}/toggle-deleting")]
        public async Task<IActionResult> ToggleDeleting(string userId, CancellationToken cancellationToken)
        {
            if (!await UserService.Exists(userId, cancellationToken))
                throw new EntityNotFoundException("User not found.");

            await UserService.ToggleDeleting(userId, cancellationToken);
            return NoContent();
        }

        [HttpGet, Route("{userId}/roles"), ResponseCache(NoStore = true)]
        public async Task<IActionResult> GetUserRoles(string userId, CancellationToken cancellationToken)
        {
            var user = await UserService.Get(userId, cancellationToken);

            if (user == null)
                throw new EntityNotFoundException("User not found.");

            return Ok(user.Roles);
        }

        [HttpGet]
        [Route("all-roles")]
        public async Task<IActionResult> GetAllRoles() => Ok(await UserService.GetAllRoles());

        [HttpGet]
        [Route("all-groups")]
        public async Task<IActionResult> GetAllGroups() => Ok(await UserService.GetAllGroups());

        [HttpGet, Route("{id}/email")]
        [AllowAnonymous]
        public async Task<IActionResult> GetEmail(string id, CancellationToken cancellationToken)
        {
            var user = await UserService.Get(id, false, true, cancellationToken);

            if (user == null)
                throw new EntityNotFoundException("User not found.");

            return Ok(user.Email);
        }

        [HttpPost, Route("me")]
        [Authorize]
        public async Task<IActionResult> UpdateLoggedUser([FromBody] UserDTO dto, CancellationToken cancellationToken)
        {
            // To ensure we update the logged user, for security purpose
            dto.Id = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Removing old avatar image
            await DeleteUserAvatarFile(dto.Id, dto, cancellationToken);

            var result = await UserService.Save(dto, cancellationToken);

            // Unbinding file from user and operation name to complete operation
            await CompleteAvatarImageUploading(cancellationToken);

            return Ok(result);
        }

        [HttpPost]
        [Route("replace-users-roles")]
        public async Task<IActionResult> ReplaceUsersRoles([FromBody] UsersRolesReplacementDTO dto, CancellationToken cancellationToken = default) =>
            Ok(await UserService.ReplaceUsersRoles(dto, cancellationToken));

        [HttpPost]
        [Route("replace-users-groups")]
        public async Task<IActionResult> ReplaceUsersGroups([FromBody] UsersGroupsReplacementDTO dto, CancellationToken cancellationToken = default) =>
            Ok(await UserService.ReplaceUsersGroups(dto, cancellationToken));

        [HttpPost, Route("upload-avatar-image")]
        [Authorize]
        [AllowedFormFileFormats(300000, "image/png", "image/jpeg", "image/gif")]
        public async Task<IActionResult> UploadLogoImage(IFormCollection formData, CancellationToken cancellationToken)
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
            additionalData.Add("operation_name", UploadAvatarImageOperationName);

            return Ok((await _fileStorageService.UploadFiles(files.ToArray(), additionalData, cancellationToken)).SuccessfullyUploadedFiles[0]);
        }


        private async Task DeleteUserAvatarFile(string id, UserDTO dto, CancellationToken cancellationToken = default)
        {
            var user = await UserService.Get(id, cancellationToken);

            if (user.AvatarImageId != null && user.AvatarImageId != dto.AvatarImageId)
                await _fileStorageService.DeleteFile((int)user.AvatarImageId, cancellationToken);
        }

        private async Task CompleteAvatarImageUploading(CancellationToken cancellationToken = default) =>
            await _fileStorageService.CompleteUsersFilesUploadingOperation(
                User.FindFirstValue(ClaimTypes.NameIdentifier), UploadAvatarImageOperationName, cancellationToken);
    }
}