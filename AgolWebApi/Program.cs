using AspNetCoreRateLimit;
using Autofac.Extensions.DependencyInjection;
using Core.Web.Extensions;
using Destructurama;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using System.Diagnostics;
using System.Reflection;

namespace Project.AgolWebApi
{
    public class Program
    {
        private static string Environment =>
            System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environments.Development;

        private static FileVersionInfo ProductInfo =>
            FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

        private static bool IsDevelop =>
            string.Equals(Environment, Environments.Development, StringComparison.OrdinalIgnoreCase);


        public static async Task Main(string[] args)
        {
            var environment = Environment;
            var productInfo = ProductInfo;
            var configuration = BuildConfiguration(args, environment, out var ebEnvironmentName);

            ConfigureLogger(configuration);

            if (!string.IsNullOrEmpty(ebEnvironmentName))
            {
                environment = ebEnvironmentName;
            }
            Log.Logger.Information($"Starting application. {productInfo.ProductName} version {productInfo.ProductVersion}. Environment: {environment}");

            if (IsDevelop)
            {
                await MainDevelopment(configuration, environment, args);
            }
            else
            {
                await MainProduction(configuration, environment, args);
            }
        }


        private static async Task MainDevelopment(IConfigurationRoot configuration, string environment, string[] args)
        {
            var programLogger = Log.Logger.ForContext<Program>();

            try
            {
                programLogger.Debug("Config: {@config}", configuration.GetChildren());
                programLogger.Information("Starting in development mode.");

                var host = CreateWebHostBuilder(configuration, true, environment, args).Build();
                
                await SeedPolicies(host);
                await host.RunAsync();

                programLogger.Information("Application quit normally.");
            }
            catch (Exception e)
            {
                programLogger.Fatal(e, "Exception occured");
            }
            finally
            {
                (programLogger as IDisposable)?.Dispose();
            }
        }

        private static async Task MainProduction(IConfigurationRoot configuration, string environment, string[] args)
        {
            var programLogger = Log.Logger.ForContext<Program>();

            try
            {
                programLogger.Debug("MainProduction()");

                var host=CreateWebHostBuilder(configuration, false, environment, args).Build();
                await SeedPolicies(host);
                await host.RunAsync();

                programLogger.Information("Application quit normally.");
            }
            catch (Exception e)
            {
                programLogger.Fatal(e, "Exception occured.");
            }
            finally
            {
                (programLogger as IDisposable)?.Dispose();
            }
        }

        private static IHostBuilder CreateWebHostBuilder(IConfigurationRoot configuration, bool detailedErrors, string environment, string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args)
               .UseServiceProviderFactory(new AutofacServiceProviderFactory())
               .UseSerilog(dispose: true)
               .ConfigureWebHostDefaults(webBuilder =>
               {
                   webBuilder.UseKestrel(options =>
                   {
                       options.Limits.MaxRequestBodySize = 104857600;
                   })
                       .CaptureStartupErrors(true)
                       .UseSetting(WebHostDefaults.DetailedErrorsKey, detailedErrors.ToString())
                       .UseConfiguration(configuration)
                       .UseEnvironment(environment)
                       .UseStartup<Startup>();
               })
               .ConfigureLogging(logging =>
               {
                   logging.AddSerilog();
               })
               .ConfigureServices(s =>
               {
                   s.AddSingleton(configuration);
               });




            return builder;
        }

        private static async Task SeedPolicies(IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                // get the ClientPolicyStore instance
                var clientPolicyStore = scope.ServiceProvider.GetRequiredService<IClientPolicyStore>();

                // seed Client data from appsettings
                await clientPolicyStore.SeedAsync();

                // get the IpPolicyStore instance
                var ipPolicyStore = scope.ServiceProvider.GetRequiredService<IIpPolicyStore>();

                // seed IP data from appsettings
                await ipPolicyStore.SeedAsync();
            }
        }

        private static IConfigurationRoot BuildConfiguration(string[] args, string environment, out string ebEnvironmentName) =>
            new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile($"appsettings.{environment}.json", true, true)
                .AddEnvironmentVariables()
                .AddEbConfig(out ebEnvironmentName)
                .Build();

        private static void ConfigureLogger(IConfiguration configuration)
        {
            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Destructure.UsingAttributes()
                .WriteTo.Console();

            if (!IsDevelop)
            {
                loggerConfig.Enrich.WithDockerContainerId();
                loggerConfig.WriteTo.RollingFile(new JsonFormatter(), "logs/bbwt3-{Date}.txt");
                loggerConfig.WriteTo.Logentries(
                    $"{configuration["LOGENTRIES_KEY"] ?? "-"}",
                    new JsonFormatter(),
                    restrictedToMinimumLevel: LogEventLevel.Information);
            }

            Log.Logger = loggerConfig.CreateLogger();
        }

        

        private static void ProcessDbCommands(string[] args, IWebHost host)
        {
            var services = (IServiceScopeFactory)host.Services.GetService(typeof(IServiceScopeFactory));

            using (var scope = services.CreateScope())
            {
                var db = scope.ServiceProvider.GetService<Project.Data.IDataContext>();

                if (args.Contains("dropdb"))
                {
                    Console.WriteLine("Dropping database");
                    db.Database.EnsureDeleted();
                }

                if (args.Contains("migratedb"))
                {
                    Console.WriteLine("Migrating database");
                    db.Database.Migrate();
                }
            }
        }
    }
}