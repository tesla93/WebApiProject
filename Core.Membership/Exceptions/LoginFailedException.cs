using Core.Exceptions;

namespace Core.Membership.Exceptions
{
    /// <summary>
    /// The exception that is thrown when a login has failed.
    /// </summary>
    public class LoginFailedException : BusinessException
    {
        /// <summary>
        /// Initialize a new instance of the <see cref="T:Core.Exceptions.LoginFailedException"></see> class with data for audit.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public LoginFailedException(string message) : base(message) { }
    }
}
