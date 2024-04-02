using System;

namespace Core.Exceptions
{
    /// <summary>
    /// Represents the base class for layer of a business-logic. Use this inside services.
    /// </summary>
    public class DuplicateException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Core.Exceptions.BusinessException"></see> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public DuplicateException(string message) : base(message) { }
        public DuplicateException(string message, Exception innerException) : base(message, innerException) { }
    }
}
