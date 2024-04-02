using Core.Data;
using FileStorage;
using Core.Membership;
using Data.SqlServer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Data
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBBWTSqlServerDataContext(
            this IServiceCollection services,
            DatabaseConnectionSettings connectionSettings,
            string connectionString,
            Action<IdentityOptions> identityOptions)

        {
            services.AddDbContext<DataContext>(options =>
                options.UseSqlServer(connectionString, builder => builder.EnableRetryOnFailure(connectionSettings)));

            services.AddScoped<IDataContext, DataContext>();
            services.AddScoped<IDbContext, DataContext>();
            services.AddScoped<IFileDetailsContext, DataContext>();

            services.AddSqlServerSignInManager<DataContext>(identityOptions);

            return services;
        }
    }
}