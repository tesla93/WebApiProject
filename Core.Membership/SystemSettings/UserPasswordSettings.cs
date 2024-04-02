namespace Core.Membership.SystemSettings
{
    /// <summary>
    /// Settings of User Password Reuse
    /// </summary>
    public enum PasswordReuseSettings
    {
        /// <summary>
        /// Users may re-use passwords
        /// </summary>
        MayUse,

        /// <summary>
        /// Users may never use a password that they previously used
        /// </summary>
        NeverUse,

        /// <summary>
        /// Users may use any password that they haven’t used in the last N passwords
        /// </summary>
        MayReUse
    }

    /// <summary>
    /// Settings of Valid Characters
    /// </summary>
    public class ValidCharacterseSettings
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ValidCharacterseSettings()
        {
            Lowercase = true;
            Uppercase = true;
            Numbers = true;
            Special = false;
        }

        /// <summary>
        /// Lowercase
        /// </summary>
        public bool Lowercase { get; set; }

        /// <summary>
        /// Uppercase
        /// </summary>
        public bool Uppercase { get; set; }

        /// <summary>
        /// Digits
        /// </summary>
        public bool Numbers { get; set; }

        /// <summary>
        /// Punctuation
        /// </summary>
        public bool Special { get; set; }
    }

    public class UserPasswordSettings
    {
        public const int DefaultPasswordResetExpireInDays = 30;
        public UserPasswordSettings()
        {
            PasswordReuse = PasswordReuseSettings.MayUse;
            ValidCharacters = new ValidCharacterseSettings();
            LastPasswordsNumber = 5;
            MinPasswordLength = 8;
            Autocomplete = false;
            Strength = 2;
        }

        /// <summary>
        /// Settings of User Password Reuse
        /// </summary>
        public PasswordReuseSettings PasswordReuse { get; set; }

        /// <summary>
        /// Settings of Valid Characters
        /// </summary>
        public ValidCharacterseSettings ValidCharacters { get; set; }

        /// <summary>
        /// The number of last passwords that a user can reuse
        /// </summary>
        public int LastPasswordsNumber { get; set; }

        /// <summary>
        /// Min Password Length
        /// </summary>
        public int MinPasswordLength { get; set; }

        /// <summary>
        /// Password saving settings (on/off autocomplete for password/email inputs)
        /// </summary>
        public bool Autocomplete { get; set; }

        /// <summary>
        /// Common password minimal strength.
        /// </summary>
        public int Strength { get; set; }
    }
}
