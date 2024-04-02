using System;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Core.Data
{
    public static class DbContextOptionsBuilderExtensions
    {
        public static MySqlDbContextOptionsBuilder EnableRetryOnFailure(this MySqlDbContextOptionsBuilder builder, DatabaseConnectionSettings connectionSettings) =>
            builder.EnableRetryOnFailure(
                connectionSettings.MaxRetryCount,
                TimeSpan.FromSeconds(connectionSettings.MaxRetryDelay),
                connectionSettings.ErrorNumbersToAdd);

        public static SqlServerDbContextOptionsBuilder EnableRetryOnFailure(this SqlServerDbContextOptionsBuilder builder, DatabaseConnectionSettings connectionSettings) =>
            builder.EnableRetryOnFailure(
                connectionSettings.MaxRetryCount,
                TimeSpan.FromSeconds(connectionSettings.MaxRetryDelay),
                connectionSettings.ErrorNumbersToAdd);
    }
}
