namespace Core.Membership.Exceptions
{
    /// <summary>
    /// The exception that is thrown when a login has failed due to wrong credentials.
    /// </summary>
    public class WrongCredentialsException : LoginFailedException
    {
        /// <summary>
        /// Initialize a new instance of the <see cref="T:Core.Exceptions.WrongCredentialsException"></see> class with data for audit.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public WrongCredentialsException(string message) : base(message) { }
    }
}
