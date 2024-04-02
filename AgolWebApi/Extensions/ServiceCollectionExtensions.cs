using AppConfiguration;
using AspNetCoreRateLimit;
using Autofac;
using AutofacExtensions;
using Core;
using Core.Audit;
using Core.Membership;
using Core.Membership.Model;
using DataProcessing;
using FileStorage;
using InitialData;
using Module.SystemSettings;
using Project.Services;
using SystemData;

namespace Server.Extensions
{
    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSpecificServices(this IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // AspNetCoreRateLimit inject counter and rules distributed cache stores
            services.AddSingleton<IIpPolicyStore, DistributedCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, DistributedCacheRateLimitCounterStore>();

            return services;
        }

        public static IServiceCollection AddFilters(this IServiceCollection services)
        {
            return services;
        }

        public static ContainerBuilder RegisterBbwtServices(this ContainerBuilder builder)
        {            
            builder.RegisterCoreServices<User>();

            builder.RegisterLoggingInterceptor();

            builder.RegisterFileStorageService();
            builder.RegisterSystemDataService();

            // Audit changes
            builder.RegisterAuditServices();

            // Data processing
            builder.RegisterDataProcessingServices();

            // System settings
            builder.RegisterSettingsService();

            

            // Project.Services
            builder.RegisterService<IApiAccessModelGetter, ApiAccessModelGetter>();
            builder.RegisterInitialDataServices();
            builder.RegisterProjectServices();

            return builder;
        }

        public static IServiceCollection ConfigureFileStorage(this IServiceCollection services, IConfiguration Configuration, IWebHostEnvironment environment)
        {
            var storageProviderName = Configuration.GetSection("StorageSettings")?.GetValue<string>("ProviderName");
            //switch (storageProviderName)
            //{
            //    case "AWS": services.ConfigureAws(Configuration); break;
            //    case "Azure": services.ConfigureAzure(Configuration); break;
            //    case "DiskSpace": services.ConfigureDiskSpaceStorageProvider(); break;
            //    default:
            //        // If specified provider not supported.
            //        if (!string.IsNullOrWhiteSpace(storageProviderName))
            //            throw new ConfigurationException($"The '{storageProviderName}' file storage provider is not supported.");

            //        // The default provider if nothing specified.
            //        if (environment.IsDevelopment())
            //            services.ConfigureDiskSpaceStorageProvider();
            //        else
            //            services.ConfigureAws(Configuration);

            //        break;
            //}

            // In case if the defined storage provider doesn't implement IAppConfigurationService
            // (like DiskStorage for local development) then we set a fake implementation in order to have
            // the app configuration end-points still be accessible (we do calls from pages)
            if (!services.Any(o => o.ServiceType == typeof(IAppConfigurationService)))
            {
                services.SetFakeAppConfigurationService();
            }

            return services;
        }
    }
}