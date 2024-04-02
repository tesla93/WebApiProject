using AutoMapper;
using AutofacExtensions;
using Core.Data;
using Core.Filters;
using Core.Membership.DTO;
using Core.Membership.Enums;
using Core.Membership.Model;
using Core.Services;
using Messages;
using Messages.Templates;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Core.Exceptions;
using Core.Extensions;
using Core.Membership.Exceptions;
using Core.ModelHashing;
using Castle.Core.Internal;
using U2F.Core.Models;
using U2F.Core.Utils;
using ClaimTypes = Core.Membership.Model.ClaimTypes;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;
using Core.Crud;
using Core.Membership.SystemSettings;
using Module.SystemSettings;
using Microsoft.Extensions.Options;
//using StaticPages;
using Microsoft.AspNetCore.Builder;

namespace Core.Membership.Services
{
    public class UserService : PagedCrudServiceBase<User, UserDTO, string>, IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly SignInManager<User> _signInManager;
        private readonly UserLoginSettings _loginSettings;
        private readonly ISecurityService _securityService;
        private readonly IEmailSender _emailSender;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IActivationTokenService _activationTokenService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IModelHashingService modelHashingService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UrlEncoder _urlEncoder;
        private readonly IPwnedPasswordProvider _pwnedPasswordProvider;
        private readonly ISettingsService _settingsService;
        public static readonly int TokenLifespanDays = 30;
        private int InvitationExpireDays
            => RegistrationSettings.DefaultUserInvitationExpireInDays;
        private int EmailConfirmationExpireInDays
            => RegistrationSettings.DefaultEmailConfirmationExpireInDays;

        private int PasswordResetExpireInDays
            => UserPasswordSettings.DefaultPasswordResetExpireInDays;


        public UserService(
            IModelHashingService modelHashingService,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<Role> roleManager,
            IOptions<UserLoginSettings> loginSettingsOptions,
            ISecurityService securityService,
            IEmailSender emailSender,
            IEmailTemplateService emailTemplateService,
            IActivationTokenService activationTokenService,
            IHttpContextAccessor httpContextAccessor,
            UrlEncoder urlEncoder,
            ICurrentUserService currentUserService,
            IPwnedPasswordProvider pwnedPasswordPrivider,
            ISettingsService settingsService,
            IDbContext context,
            IMapper mapper) : base(context, mapper)
        {
            this.modelHashingService = modelHashingService;
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _securityService = securityService;
            _loginSettings = loginSettingsOptions?.Value;
            _emailSender = emailSender;
            _emailTemplateService = emailTemplateService;
            _activationTokenService = activationTokenService;
            _httpContextAccessor = httpContextAccessor;
            _urlEncoder = urlEncoder;
            _currentUserService = currentUserService;
            _pwnedPasswordProvider = pwnedPasswordPrivider;
            _settingsService = settingsService;
        }

        #region Read methods

        public override bool UseMappingOnDb { get => false; }

        protected override IQueryable<User> GetQueryable() => _userManager.Users;

        protected override IQueryable<User> ConfigureDataReader(IQueryable<User> entities)
            => entities
                .Include(x => x.AuthenticationRequests)
                .Include(x => x.AvatarImage)
                .Include(x => x.UserGroups).ThenInclude(x => x.Group)
                .Include(x => x.UserRoles).ThenInclude(x => x.Role)
                .Include(x => x.UserPermissions).ThenInclude(x => x.Permission)
                .Include(x => x.Company).ThenInclude(x => x.Branding).ThenInclude(x => x.LogoImage)
                .Include(x => x.Company).ThenInclude(x => x.Branding).ThenInclude(x => x.LogoIcon)
                .Include(x => x.Company).ThenInclude(x => x.Address)
                .Include(x => x.DeviceRegistrations)
                .Include(x => x.EmailConfirmationToken)
                .Include(x => x.InvitationToken)
                .Include(x => x.PasswordResetToken);

        public override async Task<bool> Exists(string id, CancellationToken cancellationToken = default) => await _userManager.FindByIdAsync(id) != null;

        private string DomainUrl => $"{_httpContextAccessor.HttpContext.Request.Scheme}://{_httpContextAccessor.HttpContext.Request.Host.Value}";

        public Task<UserDTO> GetByEmail(string email, CancellationToken cancellationToken = default) =>
            GetByEmail(email, true, cancellationToken);

        public async Task<UserDTO> GetByEmail(string email, bool loadNavigationProperties, CancellationToken cancellationToken = default) =>
            Mapper.Map<UserDTO>(await (loadNavigationProperties ? ConfigureDataReader(GetQueryable()) : GetQueryable()).FirstOrDefaultAsync(x => x.Email == email, cancellationToken));

        public Task<UserDTO> GetById(string email, CancellationToken cancellationToken = default) =>
            GetById(email, true, cancellationToken);

        public async Task<UserDTO> GetById(string id, bool loadNavigationProperties, CancellationToken cancellationToken = default) =>
            Mapper.Map<UserDTO>(await (loadNavigationProperties ? ConfigureDataReader(GetQueryable()) : GetQueryable()).FirstOrDefaultAsync(x => x.Id == id, cancellationToken));

        public async Task<Enabling2FADTO> Get2FAEnablingData(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new UserNotExistsException();

            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            return new Enabling2FADTO
            {
                SharedKey = FormatKey(unformattedKey),
                AuthenticatorUri = GenerateQrCodeUri(user.Email, unformattedKey)
            };
        }

        public async Task CheckRecoveryCodeExists(RecoveryCodeDTO dto, CancellationToken cancellationToken = default)
        {
            var user = await GetQueryable()
                .Include(o => o.PasswordResetToken)
                .FirstOrDefaultAsync(x => x.Id == dto.UserId, cancellationToken);

            if (user == null) throw new UserNotExistsException();

            if (user.PasswordResetToken == null)
                throw new BusinessException(ErrorMessages.RecoveryNotFoundForUser);

            var passwordResetToken = await _activationTokenService.GetByTokenValue(dto.Code, cancellationToken);
            if (passwordResetToken == null) throw new ObjectNotExistsException("Recovery code not found.");

            if (passwordResetToken.ExpirationDate != null && passwordResetToken.ExpirationDate <= DateTime.Now)
                throw new BusinessException(ErrorMessages.RecoveryExpired);

            if (user.PasswordResetToken.Token != dto.Code)
                throw new BusinessException(ErrorMessages.RecoveryInvalid);
        }

        public async Task<AccountActivationInfoDTO> GetAccountActivationInfo(string userId, string code, CancellationToken cancellationToken = default)
        {
            var user = await GetQueryable()
                .Include(o => o.InvitationToken)
                .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
            if (user == null) throw new UserNotExistsException();

            if (user.AccountStatus != AccountStatus.Invited)
                throw new BusinessException(ErrorMessages.ActivationCompleted);

            if (user.InvitationToken == null)
                throw new BusinessException(ErrorMessages.InvitationNotFoundForUser);

            if (user.InvitationToken.Token != code)
                throw new BusinessException(ErrorMessages.ActivationCodeInvalid);

            return new AccountActivationInfoDTO
            {
                UserId = user.Id,
                Email = user.Email,
                IsInvited = user.AccountStatus == AccountStatus.Invited
            };
        }

        [IgnoreLogging(true)]
        public async Task<bool> CanImpersonate(string impersonatingUserId, string impersonatedUserId, CancellationToken cancellationToken = default)
        {
            var baseQuery = GetQueryable().Include(o => o.UserRoles).ThenInclude(o => o.Role);

            var impersonatingUser = await baseQuery.FirstOrDefaultAsync(x => x.Id == impersonatingUserId, cancellationToken);
            if (impersonatingUser == null) throw new UserNotExistsException(ErrorMessages.ImpersonatingUserNotFound);

            var impersonatedUser = await baseQuery.FirstOrDefaultAsync(x => x.Id == impersonatedUserId, cancellationToken);
            if (impersonatedUser == null) throw new UserNotExistsException(ErrorMessages.ImpersonatedUserNotFound);

            return CanImpersonate(impersonatingUser, impersonatedUser);
        }

        [IgnoreLogging(true)]
        public async Task<ImpersonateUserDTO> IsUserImpersonating(ClaimsPrincipal userClaims)
        {
            var loggedUser = await Get(userClaims.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier));
            var isImpersonating = userClaims.HasClaim(ClaimTypes.Impersonation.IsImpersonating, bool.TrueString);
            var claim = userClaims.FindFirst(ClaimTypes.Impersonation.OriginalUserName);
            var originalUserName = isImpersonating ? claim?.Value : string.Empty;

            return new ImpersonateUserDTO
            {
                ImpersonatedUserId = loggedUser != null ? loggedUser.Id : string.Empty,
                ImpersonatedUserName = loggedUser != null ? loggedUser.FullName : string.Empty,
                ImpersonatedUserEmail = loggedUser != null ? loggedUser.Email : string.Empty,
                OriginalUserName = originalUserName,
                IsImpersonating = isImpersonating
            };
        }

        public async Task<List<RoleDTO>> GetAllRoles() =>
            await Task.FromResult(Mapper.Map<List<RoleDTO>>(_roleManager.Roles
                .Include(x => x.RolePermissions)
                .ThenInclude(x => x.Permission)));

        public async Task<List<GroupDTO>> GetAllGroups(CancellationToken cancellationToken = default) =>
            Mapper.Map<List<GroupDTO>>(await GetQueryable<Group>().ToListAsync(cancellationToken));

        public Dictionary<string, object> GetAllAccountStatuses(CancellationToken cancellationToken = default) =>
            typeof(AccountStatus).GetEnumNamesValues();        

        protected override IQueryable<User> ApplyFilter(IQueryable<User> query, Filter filter)
        {
            if (filter.Filters.SingleOrDefault(x => x.PropertyName.ToLowerInvariant() == "roles") is StringFilter rolesFilter)
            {
                query = query.Where(x => x.UserRoles.Any(y => y.RoleId == rolesFilter.Value));
                filter.Filters.Remove(rolesFilter);
            }
            return base.ApplyFilter(query, filter);
        }


        private static bool CanImpersonate(User impersonatingUser, User impersonatedUser) =>
            impersonatingUser.UserRoles.Any(a => a.Role.Name == Roles.SystemAdminRole) &&
            impersonatedUser.UserRoles.All(roleItem => roleItem.Role.Name != Roles.SystemAdminRole || roleItem.Role.Name != Roles.SuperAdminRole) &&
            !string.Equals(impersonatedUser.Email, impersonatingUser.Email, StringComparison.InvariantCultureIgnoreCase);

        #endregion

        #region Edit methods

        public override async Task<UserDTO> Save(UserDTO dto, bool saveChanges = true, CancellationToken cancellationToken = default)
        {
            User user;

            dto.Email = dto.Email.Trim();
            dto.UserName = dto.UserName?.Trim();
            dto.FirstName = dto.FirstName?.Trim();
            dto.LastName = dto.LastName?.Trim();
            dto.PhoneNumber = dto.PhoneNumber?.Trim();

            if (dto.Id == null)
            {
                // Create a new user
                user = Mapper.Map<User>(dto);
                await _userManager.CreateAsync(user);
            }
            else
            {
                // Update a current user
                user = await _userManager.FindByIdAsync(dto.Id);

                var oldEmail = user.Email;

                user.FirstName = dto.FirstName;
                user.LastName = dto.LastName;
                user.Email = dto.Email;
                user.PhoneNumber = dto.PhoneNumber;
                user.UserName = dto.Email;
                user.GravatarImage = dto.GravatarImage;
                user.GravatarEmail = dto.GravatarEmail;
                user.NormalizedEmail = dto.Email.ToUpperInvariant();
                user.NormalizedUserName = dto.Email.ToUpperInvariant();
                user.CompanyId = dto.CompanyId;
                user.TwoFactorEnabled = dto.TwoFactorEnabled;
                user.U2fEnabled = dto.U2fEnabled;
                user.RecoveryCode = dto.RecoveryCode;
                user.AvatarImageId = dto.AvatarImageId;
                user.PictureMode = dto.PictureMode;
                if (user.Email != oldEmail && user.AccountStatus == AccountStatus.Active)
                    user.AccountStatus = AccountStatus.Unverified;

                await _userManager.UpdateAsync(user);

                if (user.Email != oldEmail)
                    await ResendNotificationsAfterEmailChanging(user.Id, oldEmail, cancellationToken);
            }

            // Update user roles, groups and claims
            await UpdateUserRoles(user, dto.Roles.Select(x => x.Id), cancellationToken);
            await UpdateUserPermissions(user, dto.Permissions.Select(x => x.Id), cancellationToken);
            await UpdateUserGroups(user, dto.Groups.Select(x => x.Id), cancellationToken);

            return await Get(user.Id, cancellationToken);
        }

        public override async Task Delete(string id, CancellationToken cancellationToken = default)
        {
            // https://entityframework.net/ru/knowledge-base/24271715/ validate this:
            var user = GetQueryable().FirstOrDefault(user => user.Id == id);
            if (user == null)
            {
                // throw new UserNotExistsException();
                return;
            }
            IdentityResult result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                await SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new BusinessException("User was not deleted");
            }
        }


        public async Task<UserDTO> Invite(UserDTO dto, CancellationToken cancellationToken = default)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                throw new BusinessException(existingUser.AccountStatus == AccountStatus.Deleted
                    ? ErrorMessages.EmailExistForDeleted
                    : ErrorMessages.EmailExist);

            dto.UserName = dto.Email;
            dto.AccountStatus = AccountStatus.Invited;
            var savingResult = await Save(dto, cancellationToken);

            var createdUser = await _userManager.FindByIdAsync(savingResult.Id);
            await _userManager.UpdateAsync(createdUser);
            await SaveInvitationToken(
                createdUser,
                new ActivationToken
                {
                    Token = await GeneratePasswordResetToken(createdUser),
                    ExpirationDate = DateTime.Now.AddDays(InvitationExpireDays)
                },
                cancellationToken);

            await SendInvitation(createdUser, createdUser.InvitationToken.Token, cancellationToken);

            return await Get(createdUser.Id, cancellationToken);
        }

        public async Task ResendInvitation(string userId, CancellationToken cancellationToken = default)
        {
            var user = await GetQueryable()
                .Include(o => o.InvitationToken)
                .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

            if (user == null) throw new UserNotExistsException();

            if (user.AccountStatus != AccountStatus.Invited)
                throw new BusinessException("An invitation can be re-sent only for invited users who didn't approve their invitations yet.");

            if (user.InvitationToken == null)
                throw new ConflictException("User doesn't have an invitation yet.");

            await SaveInvitationToken(
                user,
                new ActivationToken
                {
                    Token = await GeneratePasswordResetToken(user),
                    ExpirationDate = DateTime.Now.AddDays(InvitationExpireDays)
                },
                cancellationToken);

            await SendInvitation(user, user.InvitationToken.Token, cancellationToken);
        }

        public async Task<ICollection<UserDTO>> ReplaceUsersRoles(UsersRolesReplacementDTO dto, CancellationToken cancellationToken = default)
        {
            foreach (var userId in dto.UsersIds)
            {
                var originalUser = await _userManager.Users
                    .Include(x => x.UserRoles)
                    .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

                if (originalUser == null)
                    throw new UserNotExistsException(ErrorMessages.UserNotExistForId.Replace("userId", userId));

                await UpdateUserRoles(
                    originalUser,
                    originalUser.UserRoles.Select(x => x.RoleId)
                        .Union(dto.RolesIdsToAdd)
                        .Where(x => dto.RolesIdsToRemove.All(y => y != x)),
                    cancellationToken);
            }

            return Mapper.Map<ICollection<UserDTO>>(await ConfigureDataReader(GetQueryable()).Where(x => dto.UsersIds.Contains(x.Id)).ToListAsync(cancellationToken));
        }

        public async Task<ICollection<UserDTO>> ReplaceUsersGroups(UsersGroupsReplacementDTO dto, CancellationToken cancellationToken = default)
        {
            foreach (var userId in dto.UsersIds)
            {
                var originalUser = await _userManager.Users
                    .Include(x => x.UserGroups)
                    .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

                if (originalUser == null)
                    throw new UserNotExistsException(ErrorMessages.UserNotExistForId.Replace("userId", userId));

                await UpdateUserGroups(
                    originalUser,
                    originalUser.UserGroups.Select(x => x.GroupId)
                        .Union(dto.GroupsIdsToAdd.Select(x => modelHashingService.UnHashProperty<GroupDTO>(nameof(GroupDTO.Id), x)).Cast<int>())
                        .Where(x => dto.GroupsIdsToRemove.All(y => modelHashingService.UnHashProperty<GroupDTO>(nameof(GroupDTO.Id), y) != x)),
                    cancellationToken);
            }

            return Mapper.Map<ICollection<UserDTO>>(await ConfigureDataReader(GetQueryable()).Where(x => dto.UsersIds.Contains(x.Id)).ToListAsync(cancellationToken));
        }

        public async Task Register(UserRegistrationDTO dto, CancellationToken cancellationToken = default)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                throw new BusinessException($"User with email {dto.Email} already exists.");

            var user = new User
            {
                UserName = dto.Email,
                FirstName = dto.FirstName?.Trim(),
                LastName = dto.LastName?.Trim(),
                Email = dto.Email,
                CompanyId = dto.CompanyId
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                throw new BusinessException(result.Errors.First().Description);

            if (AdminApprovalRequired())
            {
                user.AccountStatus = AccountStatus.Unapproved;
                await _userManager.UpdateAsync(user);
            }
            else
            {
                user.AccountStatus = AccountStatus.Unverified;
                await _userManager.UpdateAsync(user);
                await SaveEmailConfirmationToken(
                    user,
                    new ActivationToken
                    {
                        Token = await GenerateEmailConfirmationToken(user),
                        ExpirationDate = DateTime.Now.AddDays(EmailConfirmationExpireInDays)
                    },
                    cancellationToken);
                await SendEmailConfirmation(user, user.EmailConfirmationToken.Token, cancellationToken);
            }

            await _securityService.SavePasswordToHistory(user, cancellationToken);
        }

        public async Task<int> CheckPwnedPassword(UserRegistrationDTO userRegistrationData)
        {
            var nbr = 0;
            var search = userRegistrationData.PasswordSHA1.Substring(5);

            var res = await _pwnedPasswordProvider.GetPasswordPwned(userRegistrationData.PasswordSHA1);
            if (string.IsNullOrEmpty(res)) return nbr;

            var result = res.Split("\r\n").ToList();
            foreach (var str in result)
            {
                var tab = str.Split(':');
                if (string.Equals(tab[0], search, StringComparison.CurrentCultureIgnoreCase))
                {
                    nbr = Convert.ToInt32(tab[1]);
                }
            }

            return nbr;
        }

        public async Task ResendEmailConfirmation(string userId, CancellationToken cancellationToken = default)
        {
            var user = await GetQueryable()
                .Include(o => o.EmailConfirmationToken)
                .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

            if (user == null) throw new UserNotExistsException();

            if (user.AccountStatus != AccountStatus.Unverified)
                throw new BusinessException("Repeated sending of an email confirmation is possible only for users with the \"Unverified\" status.");

            await SaveEmailConfirmationToken(
                user,
                new ActivationToken
                {
                    Token = await GenerateEmailConfirmationToken(user),
                    ExpirationDate = DateTime.Now.AddDays(EmailConfirmationExpireInDays)
                },
                cancellationToken);

            await SendEmailConfirmation(user, user.EmailConfirmationToken.Token, cancellationToken);
        }

        public async Task ResetPassword(ResetPasswordDTO dto, CancellationToken cancellationToken, bool verify = true)
        {
            if (string.IsNullOrEmpty(dto.Email))
                throw new ObjectNotExistsException(ErrorMessages.UndefinedEmail);

            var user = await GetQueryable().FirstOrDefaultAsync(x => x.Email == dto.Email, cancellationToken);
            if (user == null) throw new UserNotExistsException($"User with email {dto.Email} doesn't exist.");

            var validationMessage = await _securityService.CheckUsersNewPassword(user, dto.Password);
            if (!string.IsNullOrEmpty(validationMessage)) throw new BusinessException(validationMessage);

            if (verify)
            {
                await CheckRecoveryCodeExists(new RecoveryCodeDTO { UserId = user.Id, Code = dto.Code }, cancellationToken);

                if (user.AccountStatus != AccountStatus.Active)
                    throw new BusinessException("User is not activated.");

                var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(dto.Code));

                var result = await _userManager.ResetPasswordAsync(user, code, dto.Password);
                if (!result.Succeeded)
                    throw new BusinessException(
                        string.Join(Environment.NewLine, result.Errors.Select(x => x.Description).ToList())
                    );

                await RemovePasswordResetToken(user, cancellationToken);
            }

            if (user.LockoutEnabled)
                await _securityService.UnlockUser(user, cancellationToken);
            await _securityService.SavePasswordToHistory(user, cancellationToken);
            await SendPasswordChangedNotification(dto.Email, cancellationToken);
        }

        public async Task<AuthResultDTO> Login(LoginDTO dto, CancellationToken cancellationToken = default)
        {
            var currentIp = _currentUserService.GetUserIp();
            var user = await GetQueryable()
                .Include(o => o.UserRoles).ThenInclude(o => o.Role)
                .FirstOrDefaultAsync(u => u.Email == dto.Email, cancellationToken);

            // Whether user with specified email exist
            if (user == null)
                throw new WrongCredentialsException("Incorrect user name or password.");

            // Whether user been locked
            if (user.LockoutEnabled)
            {
                var settings = _settingsService.GetSettingsSection<FailedAttemptsPasswordSettings>();
                if (settings == null)
                    throw new ConflictException("Settings for failed login attempts not found.");

                if (user.LockoutEnd > DateTime.UtcNow || settings.UnlockTypeAccount == UnlockType.ResetPassword || user.AccountStatus == AccountStatus.Suspended)
                    throw new BusinessException("Your account is locked.");
                await _securityService.UnlockUser(user, cancellationToken);
            }

            // Whether current IP address allowed for user
            if (!await _securityService.IsIpAllowedForUser(currentIp, user.Id, cancellationToken))
                throw new BusinessException("IP address is not allowed for this user.");

            var result = new AuthResultDTO { UserId = user.Id };

            switch (user.AccountStatus)
            {
                // Check that user is invited
                case AccountStatus.Invited:
                    throw new BusinessException("An invitation has not been accepted yet.");
                // Check that user is approved
                case AccountStatus.Unapproved:
                    throw new BusinessException("Account is not approved yet.");
                // Check that user is verified
                case AccountStatus.Unverified:
                    throw new BusinessException("Email address is not verified yet.");
                // Check that user is not suspended
                case AccountStatus.Suspended:
                    throw new BusinessException("Account is suspended.");
                // Check that user is not deleted
                case AccountStatus.Deleted:
                    throw new BusinessException("Account is deleted.");
            }

            // Check password
            if (!await _userManager.CheckPasswordAsync(user, dto.Password))
                throw new WrongCredentialsException("Incorrect user name or password.");

            // Whether user is tester
            if (user.UserRoles.Any(x => x.Role.Name == Roles.RealUserEditor) &&
                string.IsNullOrEmpty(dto.RealLastName) &&
                string.IsNullOrEmpty(dto.RealFirstName) &&
                string.IsNullOrEmpty(dto.RealEmail))
            {
                result.IsRealUserEditor = true;
                return result;
            }

            // U2F
            if (user.U2fEnabled)
            {
                var deviceChallenges = await GenerateU2FDeviceAuthenticationChallenges(user.Id, DomainUrl, cancellationToken);

                var u2FAuthenticationRequest = new U2FAuthenticationRequestDTO
                {
                    AppId = DomainUrl,
                    Version = deviceChallenges[0].Version,
                    Challenge = deviceChallenges[0].Challenge,
                    RegisteredKeys = deviceChallenges
                };

                result.U2FEnabled = true;
                result.U2FAuthenticationRequest = u2FAuthenticationRequest;

                return result;
            }

            var signInResult = await _signInManager.PasswordSignInAsync(dto.Email, dto.Password, false, false);

            // 2FA
            if (signInResult.RequiresTwoFactor)
            {
                result.AuthenticatorEnabled = true;

                if (!dto.TwoFactorCode.IsNullOrEmpty())
                {
                    var authenticatorCode = dto.TwoFactorCode
                        .Replace(" ", string.Empty)
                        .Replace("-", string.Empty);

                    signInResult = await TwoFactorAuthenticatorSignIn(authenticatorCode);

                    if (!signInResult.Succeeded)
                        throw new LoginFailedException("Invalid authenticator code.");
                }
                else
                {
                    return result;
                }
            }

            // If sign in failed after successful check of the password and other checks and U2F and 2FA disabled
            if (!signInResult.Succeeded)
                throw new ConflictException("Unknown sign in error.");

            result.LoggedUser = await Get(user.Id, cancellationToken);

            return result;
        }

        public async Task Login(RecoveryCodeDTO dto, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null) throw new UserNotExistsException();

            if (user.RecoveryCode != dto.Code)
                throw new LoginFailedException(ErrorMessages.WrongRecoveryCode);

            if (user.TwoFactorEnabled)
                await Disable2Fa(user.Id, cancellationToken);
            if (user.U2fEnabled)
                await DisableU2F(user.Id, cancellationToken);

            await _signInManager.SignInAsync(user, new AuthenticationProperties(), "RecoveryCode");
        }

        public async Task<SignInResult> TwoFactorAuthenticatorSignIn(string authenticatorCode) =>
            await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, false, false);

        public async Task<SignInResult> TwoFactorRecoveryCodeSignIn(string recoveryCode) =>
            await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

        public async Task Logout() => await _signInManager.SignOutAsync();

        public async Task Activate(ResetPasswordDTO dto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(dto.Email))
                throw new ObjectNotExistsException("The email cannot be sent. Email address is undefined.");

            var user = await GetQueryable()
                .Include(o => o.InvitationToken)
                .FirstOrDefaultAsync(x => x.Email == dto.Email, cancellationToken);

            if (user == null) throw new UserNotExistsException($"User with email {dto.Email} doesn't exist.");

            if (dto.Code != user.InvitationToken.Token)
                throw new BusinessException(ErrorMessages.ActivationCodeInvalid);

            if (user.InvitationToken.ExpirationDate != null && user.InvitationToken.ExpirationDate < DateTime.Now)
                throw new BusinessException(ErrorMessages.InvitationExpired);

            // Check if user already passed activation
            if (user.AccountStatus != AccountStatus.Invited)
                throw new BusinessException(ErrorMessages.UserAlreadyActivated);

            var validationMessage = await _securityService.CheckUsersNewPassword(user, dto.Password);
            if (!string.IsNullOrEmpty(validationMessage))
                throw new BusinessException(validationMessage);

            var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(dto.Code));
            var result = await _userManager.ResetPasswordAsync(user, code, dto.Password);
            if (!result.Succeeded)
                throw new BusinessException(string.Join(Environment.NewLine,
                    result.Errors.Select(x => x.Description).ToList()));

            user.AccountStatus = AccountStatus.Active;
            await _userManager.UpdateAsync(user);
            await RemoveInvitationToken(user, cancellationToken);

            await _securityService.SavePasswordToHistory(user, cancellationToken);
        }

        public async Task<NameValueCollection> GetTagValues(string email, string url, string ipAddress)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) throw new UserNotExistsException();

            var code = await GeneratePasswordResetToken(user);

            user.PasswordResetToken = new ActivationToken { Token = code };
            await _userManager.UpdateAsync(user);

            var callbackUrl = $"{url}?userId={user.Id}&code={code}";
            var tagValues = new NameValueCollection
                {
                    {"$UserName", $"{user.FirstName} {user.LastName}"},
                    {"$UserLink", callbackUrl},
                    {"$IpAddress", ipAddress},
                };
            return tagValues;
        }

        public async Task RecoverPassword(RecoverPasswordDTO dto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(dto.Email))
                throw new ObjectNotExistsException(ErrorMessages.UndefinedEmail);

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) throw new UserNotExistsException($"User with email {dto.Email} doesn't exist.");

            if (user.AccountStatus != AccountStatus.Active)
                throw new BusinessException(ErrorMessages.UserNotActivatedForEmail.Replace("u.Email", dto.Email));

            await SavePasswordResetToken(
                user,
                new ActivationToken
                {
                    ExpirationDate = DateTime.Now.AddDays(PasswordResetExpireInDays),
                    Token = await GeneratePasswordResetToken(user)
                },
                cancellationToken);

            await SendPasswordReset(user, user.PasswordResetToken.Token, cancellationToken);
        }

        public async Task ConfirmEmail(ConfirmEmailDTO dto, CancellationToken cancellationToken = default)
        {
            var user = await GetQueryable()
                .Include(o => o.EmailConfirmationToken)
                .FirstOrDefaultAsync(x => x.Id == dto.UserId, cancellationToken);

            if (user == null) throw new UserNotExistsException();

            if (!string.IsNullOrEmpty(dto.Code) && user.EmailConfirmationToken == null)
                throw new BusinessException("Activation code is invalid. It seems that you've already used this link.");

            if (string.IsNullOrEmpty(dto.Code) || dto.Code != user.EmailConfirmationToken?.Token)
                throw new BusinessException("Activation code is invalid.");

            if (user.EmailConfirmationToken?.ExpirationDate != null && user.EmailConfirmationToken.ExpirationDate < DateTime.Now)
                throw new BusinessException("Email confirmation code has expired.");

            if (user.AccountStatus == AccountStatus.Suspended)
                throw new BusinessException("Account is suspended.");

            if (user.AccountStatus == AccountStatus.Deleted)
                throw new BusinessException("Account is deleted.");

            if (user.AccountStatus != AccountStatus.Unverified)
                throw new BusinessException("Only users with \"Unapproved\" status can be approved.");

            var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(dto.Code));
            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (!result.Succeeded)
                throw new BusinessException(string.Join(Environment.NewLine, result.Errors.Select(x => x.Description).ToList()));

            user.AccountStatus = AccountStatus.Active;
            await _userManager.UpdateAsync(user);
            await RemoveEmailConfirmationToken(user, cancellationToken);
        }

        public async Task Approve(string userId, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new UserNotExistsException();

            if (user.AccountStatus != AccountStatus.Unapproved)
                throw new BusinessException("Only users with \"Unapproved\" status can be approved.");

            user.AccountStatus = AccountStatus.Unverified;
            await _userManager.UpdateAsync(user);
            await SaveEmailConfirmationToken(
                user,
                new ActivationToken
                {
                    Token = await GenerateEmailConfirmationToken(user),
                    ExpirationDate = DateTime.Now.AddDays(EmailConfirmationExpireInDays)
                },
                cancellationToken);

            await SendEmailConfirmation(user, user.EmailConfirmationToken.Token, cancellationToken);
        }

        public async Task ToggleLocking(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new UserNotExistsException();

            if (user.AccountStatus != AccountStatus.Active &&
                user.AccountStatus != AccountStatus.Unverified &&
                user.AccountStatus != AccountStatus.Suspended)
                throw new BusinessException("Only users with \"Active\", \"Unverified\" or \"Suspended\" statuses can be locked or unlocked.");

            if (user.AccountStatus != AccountStatus.Suspended)
            {
                user.PreviousAccountStatus = user.AccountStatus;
                user.AccountStatus = AccountStatus.Suspended;
                await _userManager.SetLockoutEnabledAsync(user, true);
            }
            else
            {
                if (user.PreviousAccountStatus == null)
                    throw new ConflictException("The previous account status is unset.");

                user.AccountStatus = (AccountStatus) user.PreviousAccountStatus;
                user.PreviousAccountStatus = null;
                await _userManager.SetLockoutEnabledAsync(user, false);
            }

            await _userManager.UpdateAsync(user);
        }

        public async Task ToggleDeleting(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new UserNotExistsException();

            if (user.AccountStatus == AccountStatus.Deleted)
            {
                if (user.PreviousAccountStatus == null)
                    throw new ConflictException("The previous account status is unset.");

                user.AccountStatus = (AccountStatus) user.PreviousAccountStatus;
                user.LockoutEnabled = false;
                user.PreviousAccountStatus = null;
            }
            else
            {
                if (user.AccountStatus != AccountStatus.Suspended)
                    user.PreviousAccountStatus = user.AccountStatus;
                user.AccountStatus = AccountStatus.Deleted;
            }

            await _userManager.UpdateAsync(user);
        }

        public async Task Enable2Fa(Enabling2FADTO dto, string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new UserNotExistsException();

            // Strip spaces and hyphens
            var verificationCode = dto.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
            var is2FaTokenValid = await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);
            if (!is2FaTokenValid)
                throw new BusinessException("Verification code is invalid.");

            var result = await _userManager.SetTwoFactorEnabledAsync(user, true);
            if (!result.Succeeded)
                throw new ConflictException( $"Unexpected error occurred while enabling 2FA.");

            await _userManager.SetTwoFactorEnabledAsync(user, true);
        }

        public async Task Disable2Fa(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new UserNotExistsException();

            var result = await _userManager.SetTwoFactorEnabledAsync(user, false);
            if (!result.Succeeded)
                throw new ConflictException($"Unexpected error occurred while disabling 2FA.");
        }

        public async Task EnableU2F(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new UserNotExistsException();

            user.U2fEnabled = true;
            await _userManager.UpdateAsync(user);
        }

        public async Task DisableU2F(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new UserNotExistsException();

            user.U2fEnabled = false;
            await _userManager.UpdateAsync(user);
        }

        public async Task<string> Generate2FaOrU2FRecoveryCodes(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new UserNotExistsException();

            if (!user.TwoFactorEnabled && !user.U2fEnabled)
                throw new BusinessException(ErrorMessages.Disabled2FAOrU2F);

            if (string.IsNullOrEmpty(user.RecoveryCode))
            {
                var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
                user.RecoveryCode = string.Join("", codes.ToArray());
                await _userManager.UpdateAsync(user);
            }

            return user.RecoveryCode;
        }

        public async Task<U2FRegistrationRequestDTO> GenerateU2FDeviceRegistrationChallenge(string userId, string appUrl, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.Users
                .Include(o => o.AuthenticationRequests)
                .FirstOrDefaultAsync(o => o.Id == userId, cancellationToken);

            if (user == null) throw new UserNotExistsException();

            var startedRegistration = U2F.Core.Crypto.U2F.StartRegistration(appUrl);

            var authenticationRequest = new AuthenticationRequest
            {
                AppId = startedRegistration.AppId,
                Challenge = startedRegistration.Challenge,
                Version = U2F.Core.Crypto.U2F.U2FVersion
            };

            if (user.AuthenticationRequests == null)
                user.AuthenticationRequests = new List<AuthenticationRequest>();
            user.AuthenticationRequests.Add(authenticationRequest);
            await _userManager.UpdateAsync(user);

            return Mapper.Map<U2FRegistrationRequestDTO>(authenticationRequest);
        }

        public async Task RegisterU2FDevice(string userId, U2FRegistrationResponseDTO dto, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.Users
                .Include(o => o.AuthenticationRequests)
                .Include(o => o.DeviceRegistrations)
                .FirstOrDefaultAsync(o => o.Id == userId, cancellationToken);
            if (user == null) throw new UserNotExistsException();

            if (user.AuthenticationRequests == null || user.AuthenticationRequests.Count == 0)
                throw new BusinessException(ErrorMessages.NoU2FChallenges);

            var registerResponse = new RegisterResponse(dto.RegistrationData, dto.ClientData);
            var lastAuthenticationRequest = user.AuthenticationRequests.Last();
            var startedRegistration =
                new StartedRegistration(lastAuthenticationRequest.Challenge, lastAuthenticationRequest.AppId);
            var deviceRegistration =
                U2F.Core.Crypto.U2F.FinishRegistration(startedRegistration, registerResponse);

            user.AuthenticationRequests.Clear();
            user.DeviceRegistrations.Add(new Device
            {
                AttestationCert = deviceRegistration.AttestationCert,
                Counter = Convert.ToInt32(deviceRegistration.Counter),
                CreatedOn = DateTime.Now,
                UpdatedOn = DateTime.Now,
                KeyHandle = deviceRegistration.KeyHandle,
                PublicKey = deviceRegistration.PublicKey
            });
            await _userManager.UpdateAsync(user);
        }

        public async Task<List<U2FRegisteredKeysDTO>> GenerateU2FDeviceAuthenticationChallenges(string userId, string appUrl, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.Users
                .Include(o => o.AuthenticationRequests)
                .Include(o => o.DeviceRegistrations)
                .FirstOrDefaultAsync(o => o.Id == userId, cancellationToken);
            if (user == null) throw new UserNotExistsException();

            var devices = user.DeviceRegistrations.Where(u => !u.IsCompromised).ToList();
            if (devices.Count == 0)
                throw new BusinessException("Suitable registered devices not found.");

            user.AuthenticationRequests.Clear();
            var challenge = U2F.Core.Crypto.U2F.GenerateChallenge();
            var serverChallenges = new List<U2FRegisteredKeysDTO>();

            foreach (var registeredDevice in devices)
            {
                user.AuthenticationRequests.Add(new AuthenticationRequest
                {
                    AppId = appUrl,
                    Challenge = challenge,
                    KeyHandle = registeredDevice.KeyHandle.ByteArrayToBase64String(),
                    Version = U2F.Core.Crypto.U2F.U2FVersion
                });
                serverChallenges.Add(new U2FRegisteredKeysDTO
                {
                    Challenge = challenge,
                    KeyHandle = registeredDevice.KeyHandle.ByteArrayToBase64String(),
                    Version = U2F.Core.Crypto.U2F.U2FVersion
                });
            }

            await _userManager.UpdateAsync(user);
            return serverChallenges;
        }

        public async Task AuthenticateU2FDevice(U2FAuthenticationResponseDTO dto, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.Users
                .Include(o => o.AuthenticationRequests)
                .Include(o => o.DeviceRegistrations)
                .FirstOrDefaultAsync(o => o.Id == dto.UserId);

            if (user == null) throw new UserNotExistsException();

            if (user.AuthenticationRequests == null || user.AuthenticationRequests.Count == 0)
                throw new BusinessException("There are no U2F authentication requests for this user.");

            var authenticateResponse = new AuthenticateResponse(
                dto.ClientData,
                dto.SignatureData,
                dto.KeyHandle
            );

            var device = user.DeviceRegistrations.FirstOrDefault(d =>
                d.KeyHandle.SequenceEqual(authenticateResponse.KeyHandle.Base64StringToByteArray()));
            if (device == null)
                throw new BusinessException("The registered device not found.");

            // User will have a authentication request for each device they have registered so get the one that matches the device key handle
            var authenticationRequest =
                user.AuthenticationRequests.FirstOrDefault(f => f.KeyHandle.Equals(authenticateResponse.KeyHandle));
            if (authenticationRequest == null)
                throw new ConflictException("There are no U2F authentication request for the corresponding found device.");

            var registration = new DeviceRegistration(
                device.KeyHandle,
                device.PublicKey,
                device.AttestationCert,
                Convert.ToUInt32(device.Counter)
            );

            var authentication = new StartedAuthentication(
                authenticationRequest.Challenge,
                authenticationRequest.AppId,
                authenticationRequest.KeyHandle
            );

            U2F.Core.Crypto.U2F.FinishAuthentication(authentication, authenticateResponse, registration);
            await _signInManager.SignInAsync(user, new AuthenticationProperties(), "U2F");

            user.AuthenticationRequests.Clear();
            device.Counter = Convert.ToInt32(registration.Counter);
            device.UpdatedOn = DateTime.Now;
            await _userManager.UpdateAsync(user);
        }

        [IgnoreLogging]
        public async Task<UserDTO> Impersonate(string impersonatingUserId, string impersonatedUserId, CancellationToken cancellationToken = default)
        {
            if (!await CanImpersonate(impersonatingUserId, impersonatedUserId, cancellationToken))
                throw new BusinessException("You can not impersonate this user.");

            var impersonatingUser = await _userManager.FindByIdAsync(impersonatingUserId);
            var impersonatedUser = await _userManager.FindByIdAsync(impersonatedUserId);

            var userPrincipal = await _signInManager.CreateUserPrincipalAsync(impersonatedUser);
            userPrincipal.Identities.First()
                .AddClaim(new Claim(ClaimTypes.Impersonation.OriginalUserId, impersonatingUserId));
            userPrincipal.Identities.First().AddClaim(new Claim(ClaimTypes.Impersonation.OriginalUserName,
                $"{impersonatingUser.FirstName} {impersonatingUser.LastName}"));
            userPrincipal.Identities.First()
                .AddClaim(new Claim(ClaimTypes.Impersonation.IsImpersonating, bool.TrueString));

            await _httpContextAccessor.HttpContext.SignOutAsync();
            await _httpContextAccessor.HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, userPrincipal);

            return await Get(impersonatedUserId, cancellationToken);
        }

        [IgnoreLogging(true)]
        public async Task<UserDTO> StopImpersonation(ClaimsPrincipal userClaims, CancellationToken cancellationToken = default)
        {
            if (!userClaims.HasClaim(ClaimTypes.Impersonation.IsImpersonating, bool.TrueString))
                throw new BusinessException("User is not impersonating now.");

            var originalUserId = userClaims.FindFirstValue(ClaimTypes.Impersonation.OriginalUserId);
            if (string.IsNullOrEmpty(originalUserId))
                throw new ConflictException("Original identifier of the impersonating user is empty.");

            var originalUser = await _userManager.FindByIdAsync(originalUserId);
            if (originalUser == null)
                throw new ConflictException("User with original identifier of the impersonating user not found.");

            await _signInManager.SignOutAsync();
            await _signInManager.SignInAsync(originalUser, false);

            return await Get(originalUser.Id, cancellationToken);
        }

        public Task CreateInitialUser(UserDTO initialUser, string role = null)
            => CreateInitialUser(initialUser, role == null ? null : new[] { role }, null, null);

        public Task CreateInitialUser(UserDTO initialUser, string[] roles)
            => CreateInitialUser(initialUser, roles, null, null);

        public async Task CreateInitialUser(UserDTO initialUser, string[] roles, string[] permissions, string[] groups = null)
        {
            var isCreated = await InitializeDemoUser(initialUser);

            // If user is just created then we add his roles/permissions, otherwise we don't change existing values
            if (isCreated)
            {
                var user = await _userManager.FindByNameAsync(initialUser.Email);

                if (roles?.Length > 0)
                {
                    foreach (var role in roles)
                    {
                        await _userManager.AddToRoleAsync(user, role);
                    }
                }

                if (permissions?.Length > 0)
                {
                    foreach (var permissionName in permissions)
                    {
                        var permission = await GetQueryable<Permission>().FirstOrDefaultAsync(o => o.Name == permissionName);
                        if (permission != null)
                        {
                            permission.UserPermissions.Add(new UserPermission { UserId = user.Id, PermissionId = permission.Id });
                        }
                    }
                }

                if (groups?.Length > 0)
                {                    
                    foreach (var groupName in groups)
                    {
                        var group = await GetQueryable<Group>().FirstOrDefaultAsync(o => o.Name == groupName);
                        if (group != null)
                        {
                            group.UserGroups.Add(new UserGroup { UserId = user.Id, GroupId = group.Id });
                            await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.BelongsToGroup, group.Id.ToString()));
                        }
                    }
                }
            }

            await SaveChangesAsync();
        }

        public async Task<List<PermissionDTO>> GetAllUserPermissions(string userId, CancellationToken cancellationToken)
        {
            var permissions = await
                    (from ur in GetQueryable<UserRole>()
                     join rp in GetQueryable<RolePermission>() on ur.RoleId equals rp.RoleId
                     join p in GetQueryable<Permission>() on rp.PermissionId equals p.Id
                     where ur.UserId == userId
                     select p)
                .Concat(
                    from up in GetQueryable<UserPermission>()
                    join p in GetQueryable<Permission>() on up.PermissionId equals p.Id
                    where up.UserId == userId
                    select p)
                .ToListAsync(cancellationToken);
            return Mapper.Map<List<PermissionDTO>>(permissions);
        }

        private async Task UpdateUserClaims(User user, Dictionary<string, string> claims)
        {
            var userClaimsFromDb = await _userManager.GetClaimsAsync(user);

            foreach (var claim in claims)
            {
                var claimDb = userClaimsFromDb.FirstOrDefault(c =>
                    string.Equals(c.Type, claim.Key, StringComparison.CurrentCultureIgnoreCase));

                if (claimDb != null)
                {
                    if (claimDb.Value != claim.Value)
                    {
                        await _userManager.ReplaceClaimAsync(user, claimDb, new Claim(claim.Key, claim.Value));
                    }
                }
                else
                {
                    await _userManager.AddClaimAsync(user, new Claim(claim.Key, claim.Value));
                }
            }
        }

        private async Task UpdateUserRoles(User user, IEnumerable<string> newRolesIds, CancellationToken cancellationToken)
        {
            newRolesIds = newRolesIds.ToList();
            var existingRoles = GetQueryable<UserRole>()
                .Include(x => x.Role)
                .Where(x => x.UserId == user.Id);

            foreach (var existingRole in existingRoles)
            {
                if (newRolesIds.All(x => x != existingRole.RoleId))
                {
                    await _userManager.RemoveFromRoleAsync(user, existingRole.Role.Name);
                }
            }
            
            foreach (var newRoleId in newRolesIds.Where(n => !existingRoles.Any(e => e.RoleId == n)))
            {
                var role = await _roleManager.FindByIdAsync(newRoleId);
                // Check if this roles still exists on the DB, if so, then add
                if (role != null)
                    await _userManager.AddToRoleAsync(user, role.Name);
            }
        }

        private async Task UpdateUserPermissions(User user, IEnumerable<int> newPermissionsIds, CancellationToken cancellationToken)
        {
            newPermissionsIds = newPermissionsIds.ToList();
            var existingPermissions = GetQueryable<UserPermission>().Where(x => x.UserId == user.Id);

            foreach (var existingPermission in existingPermissions)
            {
                if (newPermissionsIds.All(x => x != existingPermission.PermissionId))
                {
                    Remove(existingPermission);
                }
            }

            foreach (var newPermissionId in newPermissionsIds)
            {
                if (await existingPermissions.AllAsync(x => x.PermissionId != newPermissionId, cancellationToken))
                {
                    await AddAsync(
                            new UserPermission
                            {
                                UserId = user.Id,
                                PermissionId = newPermissionId
                            },
                            cancellationToken);
                }
            }

            await SaveChangesAsync(cancellationToken);
        }

        private async Task UpdateUserGroups(User user, IEnumerable<int> newGroupsIds, CancellationToken cancellationToken)
        {
            newGroupsIds = newGroupsIds.ToList();
            var existingGroups = GetQueryable<UserGroup>().Where(x => x.UserId == user.Id);
            var existingUsersGroupClaims =
                (await _userManager.GetClaimsAsync(user)).Where(x => x.Type == ClaimTypes.BelongsToGroup).ToList();

            foreach (var existingGroup in existingGroups)
            {
                if (newGroupsIds.All(x => x != existingGroup.GroupId))
                {
                    Remove(existingGroup);

                    var userClaim = existingUsersGroupClaims.FirstOrDefault(x => x.Value == existingGroup.GroupId.ToString());
                    if (userClaim != null)
                        await _userManager.RemoveClaimAsync(user, userClaim);
                }
            }

            foreach (var newGroupId in newGroupsIds)
            {
                if (await existingGroups.AllAsync(x => x.GroupId != newGroupId, cancellationToken))
                {
                    await AddAsync(
                        new UserGroup
                        {
                            UserId = user.Id,
                            GroupId = newGroupId
                        },
                        cancellationToken);

                    await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.BelongsToGroup, newGroupId.ToString()));
                }
            }

            await SaveChangesAsync(cancellationToken);
        }

        private async Task SaveInvitationToken(User user, ActivationToken activationToken, CancellationToken cancellationToken)
        {
            if (activationToken == null)
            {
                if (user.InvitationToken != null)
                    await RemoveInvitationToken(user, cancellationToken);
            }
            else
            {
                if (user.InvitationToken == null)
                {
                    user.InvitationToken = activationToken;
                }
                else
                {
                    user.InvitationToken.Token = activationToken.Token;
                    user.InvitationToken.ExpirationDate = activationToken.ExpirationDate;
                }

                await _userManager.UpdateAsync(user);
            }
        }

        private async Task RemoveInvitationToken(User user, CancellationToken cancellationToken)
        {
            var oldTokenId = user.InvitationToken.Id;
            user.InvitationTokenId = null;
            user.InvitationToken = null;
            await _userManager.UpdateAsync(user);
            await _activationTokenService.Delete(oldTokenId, cancellationToken);
        }

        private async Task SavePasswordResetToken(User user, ActivationToken activationToken, CancellationToken cancellationToken)
        {
            if (activationToken == null)
            {
                if (user.PasswordResetToken != null)
                    await RemovePasswordResetToken(user, cancellationToken);
            }
            else
            {
                if (user.PasswordResetToken == null)
                {
                    user.PasswordResetToken = activationToken;
                }
                else
                {
                    user.PasswordResetToken.Token = activationToken.Token;
                    user.PasswordResetToken.ExpirationDate = activationToken.ExpirationDate;
                }

                await _userManager.UpdateAsync(user);
            }
        }

        private async Task RemovePasswordResetToken(User user, CancellationToken cancellationToken)
        {
            var oldTokenId = user.PasswordResetToken.Id;
            user.PasswordResetTokenId = null;
            user.PasswordResetToken = null;
            await _userManager.UpdateAsync(user);
            await _activationTokenService.Delete(oldTokenId, cancellationToken);
        }

        private async Task SaveEmailConfirmationToken(User user, ActivationToken activationToken, CancellationToken cancellationToken)
        {
            if (activationToken == null)
            {
                if (user.EmailConfirmationToken != null)
                    await RemoveEmailConfirmationToken(user, cancellationToken);
            }
            else
            {
                if (user.EmailConfirmationToken == null)
                {
                    user.EmailConfirmationToken = activationToken;
                }
                else
                {
                    user.EmailConfirmationToken.Token = activationToken.Token;
                    user.EmailConfirmationToken.ExpirationDate = activationToken.ExpirationDate;
                }

                await _userManager.UpdateAsync(user);
            }
        }

        private async Task RemoveEmailConfirmationToken(User user, CancellationToken cancellationToken)
        {
            var oldTokenId = user.EmailConfirmationToken.Id;
            user.EmailConfirmationTokenId = null;
            user.EmailConfirmationToken = null;
            await _userManager.UpdateAsync(user);
            await _activationTokenService.Delete(oldTokenId, cancellationToken);
        }

        private async Task ResendNotificationsAfterEmailChanging(string userId, string oldEmail, CancellationToken cancellationToken = default)
        {
            var user = await GetQueryable().FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
            if (user == null) throw new UserNotExistsException();

            if (user.AccountStatus == AccountStatus.Invited)
            {
                await ResendInvitation(userId, cancellationToken);
            }
            else
            {
                user.EmailConfirmed = false;
                user.AccountStatus = AccountStatus.Unverified;
                await _userManager.UpdateAsync(user);

                await SendEmailChangedNotification(user, oldEmail, cancellationToken);
                await ResendEmailConfirmation(userId, cancellationToken);
            }
        }

        private async Task SendInvitation(User user, string token, CancellationToken cancellationToken = default)
        {
            if (_httpContextAccessor.HttpContext == null || _emailSender == null) return;

            // Generate callbackUrl
            var callbackUrl = $"{DomainUrl}/account/activate?userId={user.Id}&code={token}";

            // Send email
            var emailTemplate =
                await GetQueryable<EmailTemplate>().FirstOrDefaultAsync(t => t.Code == "UserInvitation", cancellationToken);
            if (emailTemplate == null)
                throw new ConflictException("Email template for the user invitation not found.");

            var userInvitationTemplate = Mapper.Map<EmailTemplateDTO>(emailTemplate);
            var tagValues = new NameValueCollection
            {
                {"$UserName", $"{user.FirstName} {user.LastName}"},
                {"$CallbackUrl", callbackUrl}
            };
            _emailTemplateService.BuildEmail(userInvitationTemplate, tagValues);

            await _emailSender.SendEmail(userInvitationTemplate.Subject, userInvitationTemplate.Body, to: user.Email);
        }

        private async Task SendEmailConfirmation(User user, string token, CancellationToken cancellationToken = default)
        {
            if (_httpContextAccessor.HttpContext == null || _emailSender == null) return;

            // Generate callbackUrl
            var callbackUrl = $"{DomainUrl}/account/confirmemail?userId={user.Id}&code={token}";

            // Send email
            var emailTemplate = await _emailTemplateService.GetByCode("EmailConfirmation", cancellationToken);
            if (emailTemplate == null)
                throw new ConflictException("Email template for the email confirmation not found.");

            var tagValues = new NameValueCollection
            {
                {"$UserName", $"{user.FirstName} {user.LastName}"},
                {"$CallbackUrl", callbackUrl},
            };
            _emailTemplateService.BuildEmail(emailTemplate, tagValues);

            await _emailSender.SendEmail(emailTemplate.Subject, emailTemplate.Body, to: user.Email);
        }

        private async Task SendEmailChangedNotification(User user, string oldEmail, CancellationToken cancellationToken = default)
        {
            if (_emailSender == null) return;

            var resetPasswordTemplate = await _emailTemplateService.GetByCode("ChangeEmail_v3", cancellationToken);
            if (resetPasswordTemplate == null)
                throw new ConflictException("Email template for the notification of an email changing not found.");

            var tagValues = new NameValueCollection
            {
                {"$UserName", $"{user.FirstName} {user.LastName}"},
                {"$OldEmail", oldEmail},
                {"$NewEmail", user.Email}
            };
            _emailTemplateService.BuildEmail(resetPasswordTemplate, tagValues);

            await _emailSender.SendEmail(resetPasswordTemplate.Subject, resetPasswordTemplate.Body, to: oldEmail);
        }

        private async Task SendPasswordReset(User user, string token, CancellationToken cancellationToken = default)
        {
            if (_httpContextAccessor.HttpContext == null || _emailSender == null) return;

            // Generate callbackUrl
            var callbackUrl = $"{DomainUrl}/account/resetpassword?userId={user.Id}&code={token}";

            // Send email
            var resetPasswordTemplate = await _emailTemplateService.GetByCode("ResetPassword", cancellationToken);
            if (resetPasswordTemplate == null)
                throw new ConflictException("Email template for the password reset not found.");

            var tagValues = new NameValueCollection
            {
                {"$UserName", $"{user.FirstName} {user.LastName}"},
                {"$UserLink", callbackUrl},
                {"$IpAddress", _currentUserService.GetUserIp()}
            };
            _emailTemplateService.BuildEmail(resetPasswordTemplate, tagValues);

            await _emailSender.SendEmail(resetPasswordTemplate.Subject, resetPasswordTemplate.Body, to: user.Email);
        }

        private async Task SendPasswordChangedNotification(string email, CancellationToken cancellationToken = default)
        {
            if (_emailSender == null) return;

            if (string.IsNullOrEmpty(email))
                throw new ObjectNotExistsException("Email with a password changed notification is empty.");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                throw new UserNotExistsException("User which password is being changed not found.");

            var emailTemplate = await GetQueryable<EmailTemplate>()
                .FirstOrDefaultAsync(t => t.Code == "PasswordChanged", cancellationToken);
            if (emailTemplate == null)
                throw new ConflictException("Email template for \"Password Changed\" notification not found.");

            var passwordChangedTemplate = Mapper.Map<EmailTemplateDTO>(emailTemplate);
            var tagValues = new NameValueCollection
            {
                {"$UserName", $"{user.FirstName} {user.LastName}"}
            };

            _emailTemplateService.BuildEmail(passwordChangedTemplate, tagValues);
            await _emailSender.SendEmail(passwordChangedTemplate.Subject, passwordChangedTemplate.Body, to: email);
        }

        private async Task<string> GeneratePasswordResetToken(User user)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        }

        private async Task<string> GenerateEmailConfirmationToken(User user)
        {
            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            }
            catch
            {
                return await Task.FromResult(Guid.NewGuid().ToString());
            }
        }

        /// <summary>
        /// Defines whether a self registered user needs to be approved by admin to complete his registration in the system.
        /// 
        /// If you need to add the approval option, then rewrite this method. For example, the method could read a flag setting
        /// ([v] Admin approval  required on registration) from the system configuration settings.
        /// </summary>
        private bool AdminApprovalRequired() => true;

        private async Task<bool> InitializeDemoUser(UserDTO userDTO)
        {
            var isCreated = false;
            var passwordHasher = new PasswordHasher<User>();

            try
            {
                var user = await _userManager.FindByEmailAsync(userDTO.Email);

                if (user == null)
                {
                    user = Mapper.Map<User>(userDTO);
                    user.UserName = userDTO.Email;
                    user.EmailConfirmed = true;
                    user.AccountStatus = AccountStatus.Active;

                    var newPassword = _securityService.GetHashedValue(userDTO.Password);
                    var result = await _userManager.CreateAsync(user, newPassword.ToLowerInvariant());
                    if (result.Succeeded)
                    {
                        isCreated = true;
                        await _securityService.SavePasswordToHistory(user);
                    }
                }
                else
                {
                    var newPassword = _securityService.GetHashedValue(userDTO.Password);

                    var verifyRes = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, newPassword.ToLowerInvariant());

                    if (verifyRes == PasswordVerificationResult.Failed)
                    {
                        // Update existent demo user with a new password
                        user.PasswordHash = passwordHasher.HashPassword(user, newPassword);
                        await _userManager.UpdateAsync(user);
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return isCreated;
        }
        #endregion

        #region Helpers

        private static string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            var currentPosition = 0;
            while (currentPosition + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition, 4)).Append(" ");
                currentPosition += 4;
            }

            if (currentPosition < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition));
            }

            return result.ToString().ToLowerInvariant();
        }

        private string GenerateQrCodeUri(string email, string unformattedKey) =>
            string.Format(
                "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6",
                _urlEncoder.Encode(_loginSettings?.TwoFaAppName ?? UserLoginSettings.DefaultTwoFaAppName), _urlEncoder.Encode(email), unformattedKey
            );

        #endregion

        public static class ErrorMessages
        {
            public static string RecoveryNotFoundForUser = "Recovery code not found for the user.";
            public static string RecoveryExpired = "Recovery code has expired.";
            public static string RecoveryInvalid = "Recovery code is invalid.";
            public static string ActivationCompleted = "Activation already completed.";
            public static string ActivationCodeInvalid = "Activation code is invalid.";
            public static string InvitationNotFoundForUser = "Invitation not found for the user.";
            public static string InvitationExpired = "Invitation has expired.";
            public static string ImpersonatingUserNotFound = "Impersonating user doesn't exist.";
            public static string ImpersonatedUserNotFound = "Impersonated user doesn't exist.";
            public static string EmailExistForDeleted = "User with this email already exists and has 'deleted' status. You should undelete the user on the user details page instead.";
            public static string EmailExist = "User with this email already exists";
            public static string UserNotExistForId = "User with id userId doesn't exist.";
            public static string UserNotActivatedForEmail = "User with email u.Email is not activated";
            public static string UserAlreadyActivated = "User is already active.";
            public static string WrongRecoveryCode = "Wrong recovery code";
            public static string UndefinedEmail = "The email cannot be sent. Email address is undefined.";
            public static string Disabled2FAOrU2F = "Cannot generate recovery codes for user as he does not have 2FA or U2F enabled";
            public static string NoU2FChallenges = "There are no U2F registration challenges for this user";
        }
    }
}