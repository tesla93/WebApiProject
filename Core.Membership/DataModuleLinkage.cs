using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Data;
using Core.Membership.Model;
using Core.Membership.Services;
using Core.Membership.SystemSettings;
using Core.Membership.Utils;
using ModuleLinkage;
using Module.SystemSettings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Membership
{
    public class DataModuleLinkage : IDataModuleLinkage
    {
        private IServiceScope _serviceScope;
        private IDbContext _context;
        private RoleManager<Role> _roleManager;
        private ISettingsService _settingsService;
        private IUserService _userService;
        private IPermissionService _permissionService;
        private IRoleService _roleService;
        private IApiAccessModelGetter _apiAccessModelGetter;

        public async Task EnsureInitialData(IServiceScope serviceScope)
        {
            _serviceScope = serviceScope;

            _context = serviceScope.ServiceProvider.GetService<IDbContext>();
            _roleManager = _serviceScope.ServiceProvider.GetService<RoleManager<Role>>();
            _settingsService = _serviceScope.ServiceProvider.GetService<ISettingsService>();
            _userService = _serviceScope.ServiceProvider.GetService<IUserService>();
            _roleService = _serviceScope.ServiceProvider.GetService<IRoleService>();
            _permissionService = _serviceScope.ServiceProvider.GetService<IPermissionService>();
            _apiAccessModelGetter = _serviceScope.ServiceProvider.GetService<IApiAccessModelGetter>();            

            await CreateInitialRoles();
            await CreateInitialUsers();
            await CreateInitialCompanies();
            await CreateInitialSystemSettings();
        }

        private async Task CreateInitialRoles()
        {
            var roleNames = RolesExtractor.GetRolesNamesOfClass(typeof(Roles));

            foreach (var roleName in roleNames)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role == null)
                {
                    role = new Role(roleName) { Id = Guid.NewGuid().ToString() };
                    await _roleManager.CreateAsync(role);
                }
            }
        }

        private async Task CreateInitialUsers()
        {
            await _userService.CreateInitialUser(InitialUsers.SuperAdmin, Roles.SuperAdminRole);
            await _userService.CreateInitialUser(InitialUsers.SystemAdmin, Roles.SystemAdminRole);
        }       

        private async Task CreateInitialCompanies()
        {
            if (!_context.Set<Company>().Any(x => x.Level == 0))
            {
                _context.Set<Company>().AddRange(new List<Company> { new Company { Name = "SuperCompany", Level = 0 } });
                await _context.SaveChangesAsync();
            }
        }

        private async Task CreateInitialSystemSettings()
        {
            var appSettings = new[]
            {
                new SettingsDTO { Value = _settingsService.GetSettingsSection<UserPasswordSettings>() ?? new UserPasswordSettings() },
                new SettingsDTO { Value = _settingsService.GetSettingsSection<FailedAttemptsPasswordSettings>() ?? new FailedAttemptsPasswordSettings() },
                new SettingsDTO { Value = _settingsService.GetSettingsSection<UserSessionSettings>() ?? new UserSessionSettings() },
                new SettingsDTO { Value = _settingsService.GetSettingsSection<RegistrationSettings>() ?? new RegistrationSettings()}
            };

            await _settingsService.Save(appSettings);
        }
    }
}
