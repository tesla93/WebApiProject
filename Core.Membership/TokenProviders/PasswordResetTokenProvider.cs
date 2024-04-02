using System;
using System.Threading.Tasks;
using Core.Membership.Model;
using Core.Membership.Services;
using Core.Membership.SystemSettings;
using Module.SystemSettings;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.Membership.TokenProviders
{
    public class PasswordResetTokenProvider : DataProtectorTokenProvider<User>
    {
        public const string ProviderName = "Project - Password Reset Token Provider";
        private readonly ISettingsService _settingsService;

        public PasswordResetTokenProvider(
            IDataProtectionProvider dataProtectionProvider,
            IOptions<DataProtectionTokenProviderOptions> options,
            ISettingsService settingsService,
            ILogger<PasswordResetTokenProvider> logger)
            : base(dataProtectionProvider, options, logger)
            => _settingsService = settingsService;

        public override Task<bool> ValidateAsync(string purpose, string token, UserManager<User> manager, User user)
        {
            FixTokenLifespan(purpose);
            return base.ValidateAsync(purpose, token, manager, user);
        }

        private void FixTokenLifespan(string purpose)
            => Options.TokenLifespan = purpose switch
            {
                ResetTokenPurpose.ResetPassword => PasswordResetTokenExpireTimespan,
                ResetTokenPurpose.UserInvite => UserInvitationExpireTimespan,
                _ => TimeSpan.FromDays(UserService.TokenLifespanDays)
                // _ => Options.TokenLifespan
            };

        private TimeSpan PasswordResetTokenExpireTimespan
            => TimeSpan.FromDays(
                UserPasswordSettings.DefaultPasswordResetExpireInDays);

        private TimeSpan UserInvitationExpireTimespan
           => TimeSpan.FromDays(
               RegistrationSettings.DefaultUserInvitationExpireInDays);
    }
}