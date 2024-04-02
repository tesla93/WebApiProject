namespace Core.Membership.SystemSettings
{
    /// <summary>
    /// Defines the behavior according to which the user will be locked out of the account.
    /// </summary>
    public enum LockType
    {
        /// <summary>
        /// User are never locked out of the account due to failed attempts to enter his password.
        /// </summary>
        NeverLock = 0,

        /// <summary>
        /// User are locked out of the account after N consecutive failed attempts to enter their password within N minutes.
        /// </summary>
        AfterSeveralFailedAttempts
    }

    /// <summary>
    /// Defines the behavior according to which the user will be unlocked.
    /// </summary>
    public enum UnlockType
    {
        /// <summary>
        /// User will be locked out for N seconds.
        /// </summary>
        Temporary,

        /// <summary>
        /// User will be locked out until an administrator resets the password.
        /// </summary>
        ResetPassword
    }

    /// <summary>
    /// Failed Attempts Password configuration
    /// </summary>
    public class FailedAttemptsPasswordSettings
    {
        public FailedAttemptsPasswordSettings()
        {
            LockTypeAccount = LockType.NeverLock;
            MaxInvalidPasswordAttempts = 3;
            PasswordAttemptWindow = 5;
            IntervalInSeconds = 10;
        }

        /// <summary>
        /// Gets or sets the locks type.
        /// </summary>
        public LockType LockTypeAccount { get; set; }

        /// <summary>
        /// Gets or sets the unlocks type.
        /// </summary>
        public UnlockType UnlockTypeAccount { get; set; }

        /// <summary>
        /// The number of invalid password attempts allowed before the user is locked out.
        /// </summary>
        public int MaxInvalidPasswordAttempts { get; set; }

        /// <summary>
        /// The number of minutes in which a maximum number of invalid password attempts are allowed before the user is locked out.
        /// </summary>
        public int PasswordAttemptWindow { get; set; }

        /// <summary>
        /// The number of seconds to lock a user account after the number of password attempts exceeds the value in the MaxInvalidPasswordAttempts parameter.
        /// </summary>
        public int IntervalInSeconds { get; set; }
    }
}
