using Autofac;
using AutofacExtensions;
using Core.Data;
using Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Audit
{
    public static class ServiceCollectionExtensions
    {
        public static void RegisterAuditServices(this ContainerBuilder builder)
        {
            builder.RegisterService<IDataAuditService, DataAuditService>();
            // MultiTenancy
            builder.RegisterService<IMultiTenancyService, MultiTenancyService>();
        }

        public static IServiceCollection AddAuditSqlServerDataContext(this IServiceCollection services,
            DatabaseConnectionSettings connectionSettings, string connectionString)
        {
            services.AddDbContext<AuditDataContext>(options =>
                options.UseSqlServer(connectionString, builder => builder.EnableRetryOnFailure(connectionSettings)));

            services.AddScoped(typeof(IAuditDataContext), typeof(AuditDataContext));
            services.AddScoped(typeof(IAuditWrapper), typeof(AuditWrapper));

            return services;
        }

        public static IServiceCollection AddAuditMySQLDataContext(this IServiceCollection services,
            DatabaseConnectionSettings connectionSettings, string connectionString)
        {
            services.AddDbContext<AuditDataContext>(options =>
                options.UseMySql(connectionString, builder => builder
                        .EnableRetryOnFailure(connectionSettings)
                        .CharSetBehavior(Pomelo.EntityFrameworkCore.MySql.Infrastructure.CharSetBehavior.NeverAppend)
                    ));

            services.AddScoped(typeof(IAuditDataContext), typeof(AuditDataContext));
            services.AddScoped(typeof(IAuditWrapper), typeof(AuditWrapper));

            return services;
        }
    }
}