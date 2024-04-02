using System;
using System.Threading.Tasks;
using Core.Membership.Model;
using Core.Membership.SystemSettings;
using Project.SystemSettings;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.Membership.TokenProviders
{
    public class EmailConfirmationTokenProvider : DataProtectorTokenProvider<User>
    {
        public const string ProviderName = "BBWT3 - Email Confirmation Token Provider";
        private readonly ISettingsService _settingsService;

        public EmailConfirmationTokenProvider(
            IDataProtectionProvider dataProtectionProvider,
            IOptions<DataProtectionTokenProviderOptions> options,
            ISettingsService settingsService,
            ILogger<EmailConfirmationTokenProvider> logger)
            : base(dataProtectionProvider, options, logger)
            => _settingsService = settingsService;

        public override Task<bool> ValidateAsync(string purpose, string token, UserManager<User> manager, User user)
        {
            Options.TokenLifespan = EmailConfirmationExpireTimespan;
            return base.ValidateAsync(purpose, token, manager, user);
        }

        private TimeSpan EmailConfirmationExpireTimespan
           => TimeSpan.FromDays(
               RegistrationSettings.DefaultEmailConfirmationExpireInDays);
    }
}