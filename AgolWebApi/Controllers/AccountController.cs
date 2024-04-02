using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Core.Exceptions;
using Core.Membership.DTO;
using Core.Membership.Exceptions;
using Core.Membership.Services;
using Core.Membership.SystemSettings;
using Core.Services;
using JWT;
//using ReCaptcha;
using Module.SystemSettings;
using ClaimTypes = System.Security.Claims.ClaimTypes;
using ControllerBase = Core.Web.ControllerBase;

namespace Server.Api
{
    [Produces("application/json")]
    [Route("api/account"), ResponseCache(NoStore = true)]
    public class AccountApiController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ISecurityService _securityService;
        private readonly ICurrentUserService _currentUserService;
        //private readonly IReCaptchaService _recaptchaService;
        private readonly ILoginAuditService _auditService;
        private readonly ISettingsService _settingsService;
        private readonly IJwtService _jwtService;
        private readonly IUserPasswordFailedHistoryService _userPasswordFailedHistoryService;

        //private readonly IOptionsSnapshot<ReCaptchaSettings> _reCaptchaSettings;

        private const string Scheme = "https"; // hardcoded because Request.Scheme not reliable with load balanced architecture especially with Azure


        public AccountApiController(
            IUserService userService,
            ISecurityService securityService,
            ICurrentUserService currentUserService,
            //IReCaptchaService recaptchaService,
            ILoginAuditService auditLoginService,
            ISettingsService settingsService,
            IJwtService jwtService,
            ILogger<AccountApiController> logger,
            IUserPasswordFailedHistoryService userPasswordFailedHistoryService
            //IOptionsSnapshot<ReCaptchaSettings> reCaptchaSettings
            ) : base(logger)
        {
            _userService = userService;
            _securityService = securityService;
            _currentUserService = currentUserService;
            //_recaptchaService = recaptchaService;
            _auditService = auditLoginService;
            _jwtService = jwtService;
            _settingsService = settingsService;
            //_reCaptchaSettings = reCaptchaSettings;
            _userPasswordFailedHistoryService = userPasswordFailedHistoryService;
        }

        [HttpPost]
        [Route("recover-password")]
        [AllowAnonymous]
        public async Task<IActionResult> RecoverPassword([FromBody] RecoverPasswordDTO recoverPassword, CancellationToken cancellationToken = default)
            => await NoContent(() => _userService.RecoverPassword(recoverPassword, cancellationToken));

        [HttpPost]
        [Route("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO resetPassword, CancellationToken cancellationToken = default)
            => await NoContent(() => _userService.ResetPassword(resetPassword, cancellationToken));

        [HttpPost]
        [Route("activate")]
        [AllowAnonymous]
        public async Task<IActionResult> Activate([FromBody] ResetPasswordDTO dto, CancellationToken cancellationToken = default)
            => await NoContent(() => _userService.Activate(dto, cancellationToken));

        [HttpPost]
        [Route("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDTO dto, CancellationToken cancellationToken = default)
            => await NoContent(() => _userService.ConfirmEmail(dto, cancellationToken));

        [HttpPost]
        [Route("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] UserRegistrationDTO dto,
            CancellationToken cancellationToken = default)
        {
            var registrationSettings = _settingsService.GetSettingsSection<RegistrationSettings>();
            dto.CompanyId = registrationSettings.SelfRegisterUserCompanyId;

            await _userService.Register(dto, cancellationToken);

            Logger.LogInformation($"Registered user: {dto.Email}");

            return registrationSettings.CheckPwned
                ? Ok(await _userService.CheckPwnedPassword(dto))
                : (IActionResult)NoContent();
        }

        [HttpPost]
        [Route("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto, CancellationToken cancellationToken = default)
        {
            UserDTO user = null;
            var currentIp = _currentUserService.GetUserIp();

            try
            {
                user = await _userService.GetByEmail(dto.Email, cancellationToken);

                // Checks if current IP address is allowed
                if (!await _securityService.IsIpAllowed(currentIp, cancellationToken))
                    throw new ForbiddenException("IP address is not allowed.");

                // Checks if captcha is valid
                var validCaptcha = true;//await _recaptchaService.CheckReCaptcha(dto.CaptchaResponse);
                if (!validCaptcha) throw new WrongCaptchaException();

                var result = await _userService.Login(dto, cancellationToken);

                if (result.AuthenticatorEnabled)
                {
                    Logger.LogInformation(result.LoggedUser == null
                        ? $"The user with ID {user.Id} requires two-factor authentication."
                        : $"User with ID {user.Id} logged in with 2FA.");
                }

                if (result.U2FEnabled)
                {
                    Logger.LogInformation(result.LoggedUser == null
                        ? $"The user with ID {user.Id} requires U2F device."
                        : $"User with ID {user.Id} logged in with U2F.");
                }

                if (result.IsRealUserEditor)
                {
                    Logger.LogInformation($"The user with ID {user.Id} requires a tester data.");
                }

                if (result.LoggedUser != null)
                {
                    Logger.LogInformation($"User '{user.Email}' with ID {user.Id} logged in successfully.",
                        user.Roles.Any(x => x.Name == Core.Membership.Roles.RealUserEditor)
                            ? dto.RealEmail
                            : dto.Email);

                    await _auditService.Save(LoginAuditDTO.Create(
                        user.Email,
                        currentIp,
                        dto.Browser,
                        dto.Fingerprint,
                        null,
                        "The user logged in successfully."
                    ), cancellationToken);
                }

                return Ok(result);
            }
            catch (Exception exception)
            {
                Logger.LogDebug($"Dialog popup: {exception.Message}.");

                await _auditService.Save(LoginAuditDTO.Create(
                    dto.Email,
                    currentIp,
                    dto.Browser,
                    dto.Fingerprint,
                    null,
                    exception.Message
                ), cancellationToken);

                if (exception is WrongCredentialsException)
                {
                    if (_settingsService?.GetSettingsSection<FailedAttemptsPasswordSettings>()?.LockTypeAccount ==
                        LockType.AfterSeveralFailedAttempts)
                    {
                        if (user == null)
                            await _securityService.CheckIpLockOut(currentIp, cancellationToken);
                        else
                            await _securityService.AddFailedAttemptForUser(user.Id, cancellationToken);
                    }

                    await _userPasswordFailedHistoryService.Save(
                        new UserPasswordFailedHistoryDTO
                        {
                            email = dto.Email,
                            failedDate = DateTime.Now,
                            IpAddress = currentIp
                        }, cancellationToken);

                }

                return HandleException(exception);
            }
        }

        [HttpGet]
        [Route("token")]
        public IActionResult GetToken() => Ok(_jwtService.GenerateToken(User.Identity.Name));

        /// <summary>
        /// Returns Site Key for Google reCAPTCHA.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("site-key")]
        [AllowAnonymous]
        public IActionResult GetSiteKey()
        {
            //var reCaptchaSettings = _reCaptchaSettings.Value;
            //return Ok(reCaptchaSettings != null ? reCaptchaSettings.SiteKey : string.Empty);
            return Ok(string.Empty);
        }

        [HttpPost]
        [Route("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutDTO dto, CancellationToken cancellationToken = default)
            => await NoContent(async () =>
            {
                await _userService.Logout();
                Logger.LogInformation("User logged out successfully.");
                UserDTO user = null;
                try
                {
                    user = await _userService.GetById(dto.Id, cancellationToken);
                    var currentIp = _currentUserService.GetUserIp();
                    await _auditService.Save(LoginAuditDTO.Create(
                           user.Email,
                           currentIp,
                           dto?.Browser,
                           dto?.Fingerprint,
                           null,
                           "User logged out."
                       ), cancellationToken);
                }
                catch (Exception exception)
                {
                    Logger.LogDebug(exception.Message);
                }
               
            });

        /// <summary>
        /// Returns the number of seconds before the blocking for a user's IP address expires.
        /// </summary>
        [HttpGet]
        [Route("ip-locking-time")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLockingTimeByIp(CancellationToken cancellationToken = default)
        {
            var result = await _securityService.GetLongestActiveLockingByIp(_currentUserService.GetUserIp(), cancellationToken);

            if (result == null)
                return Ok(string.Empty);

            var timeSpan = result.LockoutEnd.Subtract(DateTime.Now);
            return Ok(timeSpan.Seconds.ToString());
        }

        /// <summary>
        /// Returns QR code and shared key to enable 2FA.
        /// </summary>
        [HttpGet]
        [Route("me/2fa-enabling-data")]
        public async Task<IActionResult> Get2FAEnablingDataForCurrentUser() =>
            Ok(await _userService.Get2FAEnablingData(User.FindFirstValue(ClaimTypes.NameIdentifier)));

        /// <summary>
        /// Enables 2FA.
        /// </summary>
        /// <param name="dto">The verification data that consists of QR code and shared key.</param>
        [HttpPost]
        [Route("me/enable-2fa")]
        public async Task<IActionResult> Enable2FAForCurrentUser([FromBody] Enabling2FADTO dto, CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await _userService.Enable2Fa(dto, userId, cancellationToken);

            Logger.LogInformation($"User with ID '{userId}' has enabled 2FA with an authenticator app.");
            return RedirectToAction(nameof(Generate2FAOrU2FRecoveryCodesForCurrentUser));
        }

        [HttpPost]
        [Route("me/disable-2fa")]
        public async Task<IActionResult> Disable2FAForCurrentUser(CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await _userService.Disable2Fa(userId, cancellationToken);

            Logger.LogInformation($"User with ID '{userId}' has disabled 2FA.");
            return NoContent();
        }

        [HttpPost]
        [Route("me/enable-u2f")]
        public async Task<IActionResult> EnableU2FForCurrentUser(CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await _userService.EnableU2F(userId, cancellationToken);

            Logger.LogInformation($"User with ID '{userId}' has enabled U2F.");
            return RedirectToAction(nameof(Generate2FAOrU2FRecoveryCodesForCurrentUser));
        }

        [HttpPost]
        [Route("me/disable-u2f")]
        public async Task<IActionResult> DisableU2FForCurrentUser(CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await _userService.DisableU2F(userId, cancellationToken);

            Logger.LogInformation($"User with ID '{userId}' has disabled U2F.");
            return NoContent();
        }

        [HttpGet]
        [Route("me/2fa-u2f-recovery-codes")]
        public async Task<IActionResult> Generate2FAOrU2FRecoveryCodesForCurrentUser(CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var recoveryCode = await _userService.Generate2FaOrU2FRecoveryCodes(userId, cancellationToken);

            Logger.LogInformation($"User with ID '{userId}' has generated new recovery codes for 2FA or U2F.");
            return Ok(recoveryCode);
        }


        /// <summary>
        /// Returns data for device registration.
        /// </summary>
        [HttpGet]
        [Route("me/generate-u2f-device-registration-challenge")]
        public async Task<IActionResult> GenerateU2FDeviceRegistrationChallengeForCurrentUser(CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Ok(await _userService.GenerateU2FDeviceRegistrationChallenge(userId, $"{Scheme}://{Request.Host}", cancellationToken));
        }

        [HttpPost]
        [Route("me/register-u2f-device")]
        public async Task<IActionResult> RegisterU2FDeviceForCurrentUser(
            [FromBody] U2FRegistrationResponseDTO u2FRegistrationResponseDto,
            CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await _userService.RegisterU2FDevice(userId, u2FRegistrationResponseDto, cancellationToken);

            Logger.LogInformation($"User with ID '{userId}' has registered U2F device.");
            return NoContent();
        }

        [HttpPost]
        [Route("authenticate-u2f-device")]
        [AllowAnonymous]
        public async Task<IActionResult> AuthenticateU2FDevice(
            [FromBody] U2FAuthenticationResponseDTO u2FAuthenticationResponseDto,
            CancellationToken cancellationToken = default)
        {
            await _userService.AuthenticateU2FDevice(u2FAuthenticationResponseDto, cancellationToken);

            Logger.LogInformation($"User with ID '{u2FAuthenticationResponseDto.UserId}' has authenticated.");
            return NoContent();
        }

        [HttpPost]
        [Route("recovery-code-login")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWithRecoveryCode([FromBody] RecoveryCodeDTO dto, CancellationToken cancellationToken = default)
        {
            UserDTO user = null;
            var currentIp = _currentUserService.GetUserIp();

            try
            {
                user = await _userService.Get(dto.UserId, cancellationToken);

                await _userService.Login(dto, cancellationToken);

                await _auditService.Save(LoginAuditDTO.Create(
                    user.Email,
                    currentIp,
                    dto.Browser,
                    dto.Fingerprint,
                    null,
                    "User successfully logged in with recovery code."
                ), cancellationToken);

                Logger.LogInformation($"User with ID '{user.Id}' successfully logged in with recovery code.");
                return Ok(user);
            }
            catch (Exception exception)
            {
                if (user != null)
                {
                    await _auditService.Save(LoginAuditDTO.Create(
                        user.Email,
                        currentIp,
                        dto.Browser,
                        dto.Fingerprint,
                        null,
                        exception.Message
                    ), cancellationToken);
                }

                return HandleException(exception);
            }
        }

        /// <summary>
        /// Checks availability of the recovery code.
        /// </summary>
        /// <param name="dto">Information about user and recovery code.</param>
        [HttpGet]
        [Route("check-recovery-code")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckRecoveryCode(RecoveryCodeDTO dto, CancellationToken cancellationToken = default)
        {
            await _userService.CheckRecoveryCodeExists(dto, cancellationToken);
            return NoContent();
        }

        [HttpGet, Route("activation-info")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAccountActivationInfo(string userId, string code, CancellationToken cancellationToken = default) =>
            Ok(await _userService.GetAccountActivationInfo(userId, code, cancellationToken));


        protected IActionResult HandleException(Exception exception)
        {
            if (exception is WrongCaptchaException || exception is LoginFailedException)
                return Unauthorized(exception.Message);

            throw exception;
        }
    }
}