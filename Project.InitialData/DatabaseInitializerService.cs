using Core.Data;
using Core.Exceptions;
using Core.Membership;
using Core.Membership.Enums;
using Core.Membership.Model;
using Core.Membership.Services;
using Core.Membership.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace InitialData
{
    public class DatabaseInitializerService : IDatabaseInitializerService
    {
        private readonly IDbContext _context;
        private ILogger<DatabaseInitializerService> _logger;
        private RoleManager<Role> _roleManager;
        private IApiAccessModelGetter _apiAccessModelGetter;
        //private IRoleGitDataService _roleGitDataService;
        private IPermissionService _permissionService;

        public DatabaseInitializerService(
            IDbContext context,
            ILogger<DatabaseInitializerService> logger,
            RoleManager<Role> roleManager,
            IApiAccessModelGetter apiAccessModelGetter,
            //IRoleGitDataService roleGitDataService,
            IPermissionService permissionService)
        {
            _context = context;
            _logger = logger;
            _roleManager = roleManager;
            _apiAccessModelGetter = apiAccessModelGetter;
            //_roleGitDataService = roleGitDataService;
            _permissionService = permissionService;
        }

        public void EnsureInitialData()
        {
            if (!AllMigrationsApplied())
            {
                _logger.LogError("DatabaseInitializerService: not all migrations applied");
                return;
            }

            // Data initialization is wrapped into try/catch to avoid any error to failure the portal starting
            try
            {
                switch (_apiAccessModelGetter.GetApiAccessModel())
                {
                    case ApiAccessModel.RoleBased: CreateInitialRoles(); break;
                    case ApiAccessModel.PermissionBased: CreateInitialPermissions(); break;
                }

                CreateInitialGroups();
                ProjectInitialData.EnsureInitialData(_context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ensure DB initial data failure");
            }

            // Sync roles & permissions from git's roles.json
            // This action is(should be) done after all initial roles & permissions of modules & project are seeded
            try
            {                
                switch (_apiAccessModelGetter.GetApiAccessModel())
                {
                    case ApiAccessModel.PermissionBased:
                        _permissionService.CleanupPermissions().Wait();
                        //_roleGitDataService.UpdateRolesFromJson().Wait();
                        break;

                    case ApiAccessModel.RoleBased:
                        // TODO: If we cleanup hardcoded roles in the roles based models then the soft coded roles of the demo module
                        // are removed, but they shouldn't. This needs to be resolved first or we don't clean up roles at all.
                        //_roleService.CleanupRoles().Wait();
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sync roles & permissions from git's roles.json");

                // This point throws a special critical exception so the startup stops the app if it's being run from
                // the migration job (with -migrate parameter)
                throw new DataInitCriticalException("Sync roles & permissions from git's roles.json", ex);
            }
        }

        private bool AllMigrationsApplied()
        {
            var applied = ((DbContext)_context).GetService<IHistoryRepository>().GetAppliedMigrations().Select(m => m.MigrationId);
            var total = ((DbContext)_context).GetService<IMigrationsAssembly>().Migrations.Select(m => m.Key);
            return !total.Except(applied).Any();
        }

        private void CreateInitialPermissions()
        {
            var allPermissionsNames = PermissionsExtractor.GetPermissionNamesOfClass(typeof(Project.Services.Permissions));

            foreach (var permissionName in allPermissionsNames)
            {
                if (_context.Set<Permission>().Any(x => x.Name == permissionName)) continue;

                var permission = new Permission(permissionName);
                _context.Set<Permission>().Add(permission);
                _context.SaveChanges();
            }
        }

        private void CreateInitialRoles()
        {
            var roleNames = RolesExtractor.GetRolesNamesOfClass(typeof(Project.Services.Roles));

            foreach (var roleName in roleNames)
            {
                var role = _roleManager.FindByNameAsync(roleName).Result;
                if (role == null)
                {
                    role = new Role(roleName) { Id = Guid.NewGuid().ToString() };
                    _roleManager.CreateAsync(role).Wait();
                }
            }
        }

        private void CreateInitialGroups()
        {
            var allGroupsNames = GroupsExtractor.GetGroupNamesOfClass(typeof(Project.Services.Groups));

            foreach (var groupName in allGroupsNames)
            {
                if (_context.Set<Group>().Any(x => x.Name == groupName)) continue;

                var group = new Group { Name = groupName };
                _context.Set<Group>().Add(group);
                _context.SaveChanges();
            }
        }
    }
}