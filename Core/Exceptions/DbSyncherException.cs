using System;

namespace Core.Exceptions
{
    /// <summary>
    /// Represents the exception class of DbSyncher.
    /// </summary>
    public class DbSyncherException : BusinessException
    {
        public DbSyncherException(string message) : base(message) { }
        public DbSyncherException(string message, Exception innerException) : base(message, innerException) { }
    }
}
