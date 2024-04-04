using AspNetCoreRateLimit;
using Autofac;
using Core;
using Core.Audit;
using Core.Data;
using Core.Exceptions;
using Core.ModelHashing;
using Core.Web.Filters;
using Core.Web.Middlewares;
using Core.Web.ModelBinders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Module.SystemSettings;
using ModuleLinkage;
using Newtonsoft.Json;
using Project.Data;
using Project.Data.SqlServer;
using Project.InitialData;
using Project.Server.Extensions;
using System.Globalization;
using ClaimTypes = Core.Membership.Model.ClaimTypes;
using Microsoft.AspNet.OData.Extensions;

namespace Project.AgolWebApi
{
    public partial class Startup
    {
        private IWebHostEnvironment Environment { get; }
        private IConfiguration Configuration { get; }
        public IContainer Container { get; private set; }
        public IApplicationBuilder _app { get; private set; }

        // Logs buffer is used to collect logs before the Configure() method. Core 3 allows the logger to be used by Configure() only.
        private readonly List<Tuple<DateTime, string, Exception>> LogsBuffer = new List<Tuple<DateTime, string, Exception>>();

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Environment = env;

            // for the ExcelDataReader library working
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        DatabaseConnectionSettings GetConnectiongSettings() =>
            Configuration.GetSection("DatabaseConnectionSettings").Get<DatabaseConnectionSettings>();

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            try
            {
                AddBufferLog("Started ConfigureServices()");

                services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
                services.AddSignalR();


                // configure Identity options
                void IdentityOptions(IdentityOptions options)
                {
                    options.SignIn.RequireConfirmedEmail = false;

                    // Lockout settings by default
                    options.Lockout.AllowedForNewUsers = false;
                    options.Lockout.MaxFailedAccessAttempts = 100;

                    // options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);

                    // Password settings
                    options.Password.RequireDigit = false;
                    options.Password.RequiredLength = 0;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireLowercase = false;

                    // User settings
                    // options.User.RequireUniqueEmail = true;
                }

                // configure refresh impersonation data (https://tech.trailmax.info/2017/07/user-impersonation-in-asp-net-core/)
                services.Configure<SecurityStampValidatorOptions>(options => // different class name
                {
                    options.ValidationInterval = TimeSpan.FromMinutes(10); // new property name
                    options.OnRefreshingPrincipal = context => // new property name
                    {
                        var originalUserIdClaim = context.CurrentPrincipal.FindFirst(ClaimTypes.Impersonation.OriginalUserId);
                        var isImpersonatingClaim = context.CurrentPrincipal.FindFirst(ClaimTypes.Impersonation.IsImpersonating);
                        var originalUserNameClaim = context.CurrentPrincipal.FindFirst(ClaimTypes.Impersonation.OriginalUserName);

                        if (isImpersonatingClaim == null || isImpersonatingClaim.Value != bool.TrueString || originalUserIdClaim == null) return Task.FromResult(0);

                        context.NewPrincipal.Identities.First().AddClaim(originalUserIdClaim);
                        context.NewPrincipal.Identities.First().AddClaim(originalUserNameClaim);
                        context.NewPrincipal.Identities.First().AddClaim(isImpersonatingClaim);
                        return Task.FromResult(0);
                    };
                });

                #region DB contexts
                var databaseConnectionSettings = GetConnectiongSettings();
                AddBufferLog($"Configuration's database type: {databaseConnectionSettings.DatabaseType}");
                string connectionString;
                string auditConnection;

                if (databaseConnectionSettings.DatabaseType == DatabaseType.MsSql)
                {
                    connectionString = Configuration.GetConnectionString("DefaultConnection");
                    auditConnection = Configuration.GetConnectionString("AuditConnection");

                    services.AddAuditSqlServerDataContext(databaseConnectionSettings, auditConnection);
                    services.AddBBWTSqlServerDataContext(databaseConnectionSettings, connectionString, IdentityOptions);
                }
                #endregion

                if (Environment.IsDevelopment())
                {
                    services.AddCors();
                }

                services.AddSpecificServices();
                services.AddFilters();
                services.AddOptions();
                services.ConfigureFileStorage(Configuration, Environment);

                // Angular's default header name for sending the XSRF token.
                services.AddAntiforgery(options =>
                {
                    options.HeaderName = "X-XSRF-TOKEN";
                });

                // AspNetCoreRateLimit
                // needed to store rate limit counters and ip rules
                services.AddMemoryCache();

                // load general configuration from appsettings.json
                services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));

                // load ip rules from appsettings.json
                services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));

                // load general configuration from appsettings.json
                services.Configure<ClientRateLimitOptions>(Configuration.GetSection("ClientRateLimiting"));

                // load client rules from appsettings.json
                services.Configure<ClientRateLimitPolicies>(Configuration.GetSection("ClientRateLimitPolicies"));

                // https://github.com/aspnet/Hosting/issues/793
                // the IHttpContextAccessor service is not registered by default.
                // the clientId/clientIp resolvers use it.
                services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

                // configure the resolvers
                services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

                // inject counter and rules stores
                services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
                services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
                services.AddSingleton<IClientPolicyStore, MemoryCacheClientPolicyStore>();
                var modelHashingService = new ModelHashingService();
                services.AddSingleton<IModelHashingService>(modelHashingService);

                // Set the default authentication policy to require users to be authenticated.
                // You can opt out of authentication at the controller or action method with the [AllowAnonymous] attribute.
                // With this approach, any new controllers added will automatically require authentication,
                // which is safer than relying on new controllers to include the [Authorize] attribute.
                // services.AddRouting(options => options.LowercaseUrls = true);
                services.AddResponseCaching();

                services.AddControllersWithViews(config =>
                {
                    var policy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();
                    config.Filters.Add(new GlobalRestrictedAuthorizeFilter(policy, new[] { typeof(ReadWriteAuthorizeAttribute) }));
                    config.Filters.Add(new ApiVersionAttribute(Environment));
                    config.Filters.Add(new ResponseCacheAttribute { NoStore = true, Location = ResponseCacheLocation.None });

                    // Replaces "<key>_original" propertyName-s to "<key>"
                    config.AddOriginalFiltersFixingValueProvider();

                    config.ModelBinderProviders.Insert(0, new FilterInfoModelBinderProvider());
                    config.ModelBinderProviders.Insert(1, new IdBinderProvider());
                    config.ModelBinderProviders.Insert(2, new FormDataJsonBinderProvider());

                    config.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
                })
                    .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

                services.AddControllers().AddNewtonsoftJson(options =>
                {
                    // Don't remove the error detections! Removing of error detections leads to stack overflow and server crash.
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Error;
                    var converter = new GlobalHashKeyJsonConverter(JsonSerializer.CreateDefault(options.SerializerSettings), modelHashingService);
                    options.SerializerSettings.Converters.Add(converter);

                    options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;

                    options.SerializerSettings.RegisterSerializerSettings();
                });

                services.AddOData();

                services.AddSpaStaticFiles(options =>
                {
                    options.RootPath = "wwwroot";
                });

                var authBuilder = services.AddAuthentication();

                

                // Response compression
                services.Configure<GzipCompressionProviderOptions>(options => options.Level = System.IO.Compression.CompressionLevel.Optimal);
                services.AddResponseCompression(options =>
                {
                    options.MimeTypes = new[]
                    {
                    // Default
                    "text/plain",
                    "text/css",
                    "application/javascript",
                    "text/html",
                    "application/xml",
                    "text/xml",
                    "application/json",
                    "text/json",
                    // Custom
                    "image/svg+xml"
                    };
                });

                services.ConfigureSecurity();

                #region Automapper
                var bbAssemblies = ModuleLinker.GetBbAssemblies();
                services.AddAutoMapper(cfg => ProfileBase.CollectAndRegisterMappings(cfg), bbAssemblies);
                services.AddAutoMapper(bbAssemblies);
                #endregion

                #region DB contexts of modules
                var dbContextLinkers = ModuleLinker.GetInstances<IDbContextModuleLinkage>();
                dbContextLinkers.ForEach(o =>
                {
                    try { o.AddDbContext(databaseConnectionSettings, Configuration, services); }
                    catch (Exception ex)
                    {
                        ModuleLinker.AddInvokeException(ex);
                        AddBufferLog($"{o.GetType()} exception", ex);
                    }
                });
                #endregion

                Func<IServiceProvider> getAppBuilder = () => _app.ApplicationServices;

                #region Link modules services

                var linkers = ModuleLinker.GetInstances<IServicesModuleLinkage>();
                linkers.ForEach(o =>
                {
                    try
                    {
                        o.ConfigureServices(services, Configuration);
                    }
                    catch (Exception ex)
                    {
                        ModuleLinker.AddInvokeException(ex);
                        AddBufferLog($"{o.GetType()} exception", ex);
                    }
                });

                var authenticationLinkers = ModuleLinker.GetInstances<IAuthenticationModuleLinkage>();
                authenticationLinkers.ForEach(o =>
                {
                    try
                    {
                        o.Register(authBuilder, services, Configuration, getAppBuilder);
                    }
                    catch (Exception ex)
                    {
                        ModuleLinker.AddInvokeException(ex);
                        AddBufferLog($"{o.GetType()} exception", ex);
                    }
                });

                #endregion

                AddBufferLog("Finished ConfigureServices()");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                AddBufferLog($"ConfigureServices() general exception", e);
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostApplicationLifetime applicationLifetime, ILogger<Startup> logger)
        {
            FlushLogsBuffer(logger);

            logger.LogDebug("Before app.UseSecurityHeaders()");
            var policyCollection = new HeaderPolicyCollection()
                .AddFrameOptionsSameOrigin()
                .AddContentTypeOptionsNoSniff()
                .AddXssProtectionBlock()
                .RemoveCustomHeader("X-Powered-By");
            app.UseSecurityHeaders(policyCollection);

            logger.LogDebug("Before app.UseIpRateLimiting()");
            app.UseIpRateLimiting();
            app.UseSwagger();
            app.UseSwaggerUI();


            app.UseErrorHandlingMiddleware();

            if (!Environment.IsDevelopment())
            {
                //app.UseRaygun();
            }


            // Applies migrations and data seeding
            InitDatabases(app, applicationLifetime, logger);

            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                serviceScope.InitRouteRoles();
            }

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
            });

            logger.LogDebug("Configuring locale");
            var locale = Configuration["SiteLocale"];
            if (!string.IsNullOrWhiteSpace(locale))
            {
                var localizationOptions = new RequestLocalizationOptions
                {
                    SupportedCultures = new List<CultureInfo> { new CultureInfo(locale) },
                    SupportedUICultures = new List<CultureInfo> { new CultureInfo(locale) },
                    DefaultRequestCulture = new RequestCulture(locale)
                };
                app.UseRequestLocalization(localizationOptions);
            }

            if (Convert.ToBoolean(Configuration["ENABLE_HTTPS_REDIRECT"]))
            {
                logger.LogDebug("Adding UseHttpToHttpsRedirectMiddleware");
                app.UseHttpToHttpsRedirectMiddleware();
            }

            app.UseRouting();

            logger.LogDebug("Adding middleware");
            app.UseResponseCompression();

            if (Environment.IsDevelopment())
            {
                app.UseCors(p =>
                {
                    p.AllowAnyHeader();
                    p.AllowAnyMethod();
                    p.AllowCredentials();
                    p.WithOrigins("http://localhost:4200");
                });
            }

            logger.LogDebug("Before app.UseAuthentication()");
            app.UseAuthentication();

            app.UseAuthorization();

            // TODO

            #region Configure BB modules
            var configureLinkers = ModuleLinker.GetInstances<IConfigureModuleLinkage>();
            configureLinkers.ForEach(o =>
            {
                try { o.ConfigureModule(app); }
                catch (Exception ex)
                {
                    ModuleLinker.AddInvokeException(ex);
                    logger.LogDebug(ex, $"{o.GetType()} exception");
                }
            });
            #endregion

            logger.LogDebug("Adding UseAddUserIdToLogsMiddleware()");
            app.UseAddUserToLogsMiddleware();

            app.UseStaticFiles();

            if (Environment.IsDevelopment())
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(Path.Combine(Environment.ContentRootPath, "data/images")),
                    RequestPath = "/data/images"
                });
            }

            app.UseSpaStaticFiles();
            app.UseAntiforgeryToken();

            logger.LogDebug("Before app.UseEndpoints()");
            app.UseEndpoints(endpoints =>
            {
                // Add SignalR hubs here using MapHub() method
                #region Link modules SignalR hubs
                var signalRlinkers = ModuleLinker.GetInstances<ISignalRModuleLinkage>();
                signalRlinkers.ForEach(o => o.MapHubs(endpoints));
                #endregion

                endpoints.MapControllerRoute("default", "{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = Environment.IsDevelopment() ? "./" : "wwwroot";

                if (Environment.IsDevelopment())
                {
                    spa.UseProxyToSpaDevelopmentServer("http://localhost:4200");
                }
            });

            app.UseResponseCaching();

            _app = app;

            applicationLifetime.ApplicationStopped.Register(() =>
            {
                Container.Dispose();
            });

        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            try
            {
                AddBufferLog("Started ConfigureContainer()");

                builder.RegisterBbwtServices();

                var linkers = ModuleLinker.GetInstances<IDependenciesModuleLinkage>();
                linkers.ForEach(o =>
                {
                    try { o.RegisterDependencies(builder); }
                    catch (Exception ex)
                    {
                        ModuleLinker.AddInvokeException(ex);
                        AddBufferLog($"{o.GetType()} exception", ex);
                    }
                });

                AddBufferLog("Finished ConfigureContainer()");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                AddBufferLog($"ConfigureContainer() general exception", e);
            }
        }

      

        private void InitDatabases(IApplicationBuilder app, IHostApplicationLifetime applicationLifetime, ILogger<Startup> logger)
        {
            var isDevelopment = Environment.IsDevelopment();

            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var dataContext = serviceScope.ServiceProvider.GetService<IDataContext>();
                var auditContext = serviceScope.ServiceProvider.GetService<IAuditDataContext>();

                // In the future it's planned to call the app from CI in the migration job with a purpose to apply migrations using
                // the block "Migrations" below. For now this flag (migrationViaGitPipelineEnabled) disables this option in order to
                // apply migrations either a) with CLI commands in the CI job or b) in runtime on the app start after deployment.
                const bool migrationViaGitPipelineEnabled = false;

                // With migrationViaGitPipelineEnabled = true, the "migrate" parameter should be passed from the migration CI job
                // to run the app only to apply migrations and exit.
                var shouldMigrate = Configuration.GetValue<bool>("migrate");

                // Migrations & Seeding
                if (!migrationViaGitPipelineEnabled || shouldMigrate || isDevelopment)
                {
                    #region Migrations
                    // Main database
                    var mainDatabase = dataContext.Database;
                    try
                    {
                        mainDatabase.Migrate();
                    }
                    catch (Exception ex)
                    {
                        var exMessage = @$"Main database migration failure.
                                                    
                            DATABASE NAME: {mainDatabase.GetDbConnection().Database}
                            DATABASE TYPE: {GetConnectiongSettings().DatabaseType}

                            APPLIED MIGRATIONS:
                            [ {string.Join(",  ", mainDatabase.GetAppliedMigrations().ToArray())} ]

                            PENDING MIGRATIONS:
                            [ {string.Join(",  ", mainDatabase.GetPendingMigrations().ToArray())} ]";

                        // DATABASE LOGS:
                        //    >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
                        //    [ { string.Join("\n", MyLoggerProvider.LogsBuffer.ToArray())} ]
                        //    <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<

                        throw new Exception(exMessage, ex);
                    }

                    // Audit database
                    auditContext.Database.EnsureCreated();

                    // Migrations of modules
                    var migrateLinkers = ModuleLinker.GetInstances<IDbMigrateModuleLinkage>();
                    migrateLinkers.ForEach(o =>
                    {
                        try { o.Migrate(serviceScope).Wait(); }
                        catch (Exception ex)
                        {
                            ModuleLinker.AddInvokeException(ex);
                            logger.LogDebug(ex, $"{o.GetType()} exception");
                        }
                    });
                    #endregion

                    #region Data seeding 
                    var dataLinkers = ModuleLinker.GetInstances<IDataModuleLinkage>();
                    dataLinkers.ForEach(o =>
                    {
                        try { o.EnsureInitialData(serviceScope).Wait(); }
                        catch (Exception ex)
                        {
                            ModuleLinker.AddInvokeException(ex);
                            logger.LogDebug(ex, $"{o.GetType()} exception");
                        }
                    });

                    try
                    {
                        serviceScope.EnsureInitialProjectData();
                    }
                    catch (DataInitCriticalException ex)
                    {
                        ModuleLinker.AddCommonException(ex);
                        // TODO: stop application with error if shouldMigrate flag is set
                    }
                    #endregion                    
                }

                if (migrationViaGitPipelineEnabled && shouldMigrate && !isDevelopment)
                {
                    // Stop if migration was invoked manually, and not dev env
                    applicationLifetime.StopApplication();
                }
            }
        }

        private void AddBufferLog(string message, Exception ex = null) =>
            LogsBuffer.Add(new Tuple<DateTime, string, Exception>(DateTime.Now, message, ex));

        private void FlushLogsBuffer(ILogger<Startup> logger)
        {
            LogsBuffer.ForEach(o =>
            {
                if (o.Item3 == null)
                    logger.LogDebug($"[{o.Item1}] {o.Item2}");
                else
                    logger.LogDebug(o.Item3, $"[{o.Item1}] {o.Item2}");
            });
            LogsBuffer.Clear();
        }
    }
}