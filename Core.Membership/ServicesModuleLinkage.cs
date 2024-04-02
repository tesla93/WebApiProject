using Autofac;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using AutofacExtensions;
using Core.Membership.Authorization;
using Core.Membership.Model;
using Core.Membership.Services;
using Core.Membership.DTO;
using Microsoft.Extensions.Configuration;
using Core.Exceptions;
using ModuleLinkage;
using Core.Crud.Interfaces;
using Core.Membership.SystemSettings;

namespace Core.Membership
{
    public class ServiceModuleLinkage : IServicesModuleLinkage, IDependenciesModuleLinkage
    {
        private string membershipSection = "MembershipSettings";
        private string userLoginSection = "UserLoginSettings";

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IUserClaimsPrincipalFactory<User>, UserClaimsPrincipalFactory>();

            // Config file secions
            var section = configuration.GetSection(membershipSection);
            if (section.Get<MembershipSettings>() == null)
                throw new EmptyConfigurationSectionException(membershipSection);
            if (string.IsNullOrWhiteSpace(section.Get<MembershipSettings>().RolesFilePath))
                throw new EmptyConfigurationSectionException($"{membershipSection}.{nameof(MembershipSettings.RolesFilePath)}");
            services.Configure<MembershipSettings>(section);

            section = configuration.GetSection(userLoginSection);
            services.Configure<UserLoginSettings>(section);

            // TokenLifespan is now set for password reset and user invite.
            // See EmailConfirmationTokenProvider and PasswordResetTokenProvider
            // services.Configure<DataProtectionTokenProviderOptions>(options =>
            // {
            //     options.TokenLifespan = System.TimeSpan.FromDays(UserService.TokenLifespanDays);
            // });
            services.Configure<DataProtectionTokenProviderOptions>(options => { });
        }

        public void RegisterDependencies(ContainerBuilder builder)
        {
            builder.RegisterService<IRouteRolesService, RouteRolesService>();

            // Users
            builder.RegisterService<IUserService, UserService>();
            builder.RegisterService<IPwnedPasswordProvider, PwnedPasswordProvider>();

            // User password failed history
            builder.RegisterService<IUserPasswordFailedHistoryService, UserPasswordFailedHistoryService>();

            // ActivationTokens
            builder.RegisterService<IActivationTokenService, ActivationTokenService>();

            // Roles
            builder.RegisterService<IRoleService, RoleService>();

            // Roles Git Data
            //builder.RegisterService<IRoleGitDataService, RoleGitDataService>();

            // Permissions
            builder.RegisterService<IPermissionService, PermissionService>();

            // Companies
            builder.RegisterService<ICompanyService, CompanyService>();

            // Security
            builder.RegisterService<ISecurityService, SecurityService>();

            // Branding
            builder.RegisterService<IBrandingService, BrandingService>();

            // Allow ip
            builder.RegisterService<IPagedCrudService<AllowedIpDTO>, AllowIpService>();

            // Audit login
            builder.RegisterService<ILoginAuditService, LoginAuditService>();

            Module.SystemSettings.ServiceCollectionExtensions.RegisterSection<UserPasswordSettings>("UserPasswordSettings");
            Module.SystemSettings.ServiceCollectionExtensions.RegisterSection<FailedAttemptsPasswordSettings>("FailedAttemptsPassword");
            Module.SystemSettings.ServiceCollectionExtensions.RegisterSection<UserSessionSettings>("UserSessionSettings");
            Module.SystemSettings.ServiceCollectionExtensions.RegisterSection<RegistrationSettings>("RegistrationSettings");
        }
    }
}