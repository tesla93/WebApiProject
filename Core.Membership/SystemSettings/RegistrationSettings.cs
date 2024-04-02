namespace Core.Membership.SystemSettings
{
    public class RegistrationSettings
    {
        public const int DefaultUserInvitationExpireInDays = 30;
        public const int DefaultEmailConfirmationExpireInDays = 30;
        public RegistrationSettings() {}


        /// <summary>
        /// Check on https://haveibeenpwned.com/
        /// </summary>
        public bool CheckPwned { get; set; }

        public int? SelfRegisterUserCompanyId { get; set; }

        public string WelcomeMessage { get; set; } = "Please complete the following information and then press Register.";

        /// <summary>
        /// Gets or sets how many days a User Invitation token is valid
        /// </summary>
        // These are hardcoded because tests fail
        // public int? UserInvitationExpireInDays { get; set; } = DefaultUserInvitationExpireInDays;

        // public int? EmailConfirmationExpireInDays { get; set; } = DefaultEmailConfirmationExpireInDays;
    }
}
