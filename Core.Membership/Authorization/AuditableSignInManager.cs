using System;
using System.Threading.Tasks;
using Core.Data;
using Core.Membership.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.Membership.Authorization
{
    public class AuditableSignInManager<TUser> : SignInManager<TUser> where TUser : IdentityUser
    {
        private readonly UserManager<TUser> _userManager;
        private readonly IDbContext _context;
        private readonly IHttpContextAccessor _contextAccessor;

        public AuditableSignInManager(
            UserManager<TUser> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<TUser> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<SignInManager<TUser>> logger,
            IAuthenticationSchemeProvider authenticationSchemeProvider,
            IUserConfirmation<TUser> userConfirmation,
            IDbContext context)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, authenticationSchemeProvider, userConfirmation)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public override async Task<SignInResult> PasswordSignInAsync(TUser user, string password, bool isPersistent, bool lockoutOnFailure)
        {
            var result = await base.PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure);

            if (user != null) // We can only log an audit record if we can access the user object and it's ID
            {
                var ip = _contextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();

                LoginAudit auditRecord = null;

                switch (result.ToString())
                {
                    case "Succeeded":
                        auditRecord = AuditFactory.CreateAuditEvent(user, ip, "Logged In");
                        break;

                    case "Failed":
                        auditRecord = AuditFactory.CreateAuditEvent(user, ip, "Failed Login");
                        break;

                    default:
                        // Other sign in results are not handled
                        break;
                }

                if (auditRecord != null)
                {
                    _context.Set<LoginAudit>().Add(auditRecord);
                    await _context.SaveChangesAsync();
                }
            }

            return result;
        }

        public override async Task SignOutAsync()
        {
            await base.SignOutAsync();

            var user = await _userManager.GetUserAsync(_contextAccessor.HttpContext.User);

            if (user != null)
            {
                var ip = _contextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();

                var auditRecord = AuditFactory.CreateAuditEvent(user, ip, "Signed Out");
                _context.Set<LoginAudit>().Add(auditRecord);
                await _context.SaveChangesAsync();
            }
        }
    }

    public static class AuditFactory
    {
        public static LoginAudit CreateAuditEvent<TUser>(TUser user, string ip, string result) where TUser: IdentityUser =>
            LoginAudit.Create(user.Email, ip, null, null, null, result);
    }
}