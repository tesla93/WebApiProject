using Core;
using Core.Membership;
using Core.Membership.DTO;
using Core.Membership.Enums;
using Core.Membership.Services;
using Microsoft.Extensions.DependencyInjection;
using ModuleLinkage;

namespace Project.InitialData
{
    public static class RouteRolesDataService
    {
        public static void InitRouteRoles(IServiceScope serviceScope)
        {
            var _routeRolesService = serviceScope.ServiceProvider.GetService<IRouteRolesService>();
            var _apiAccessModelGetter = serviceScope.ServiceProvider.GetService<IApiAccessModelGetter>();

            var result = new List<PageInfoDTO>
            {
                new PageInfoDTO(CoreRoutes.Home, AggregatedRole.Anyone),

                new PageInfoDTO("/app/system", "System Configuration", new List<string> {Roles.SystemAdminRole, Roles.SuperAdminRole}),
                new PageInfoDTO("/app/email-templates", "Email Templates", Roles.SystemAdminRole),
                new PageInfoDTO("/app/email-templates/edit/:id", "Edit Email Template", Roles.SystemAdminRole),

                new PageInfoDTO("/app/admin/login-audit", "Login Audit",  Roles.SystemAdminRole),
                new PageInfoDTO("/app/admin/data-audit", "Data Audit",  Roles.SystemAdminRole),

                new PageInfoDTO("/app/report-problem", "Report a Problem", AggregatedRole.Authenticated),
                new PageInfoDTO("/app/profile", "Profile", AggregatedRole.Authenticated),
                new PageInfoDTO("/app/profile/authentication", "Authentication", AggregatedRole.Authenticated),
                new PageInfoDTO("/app/routes", "View Route Access", new List<string> {Roles.SystemAdminRole, Roles.SuperAdminRole }),
                new PageInfoDTO("/app/static/pages", "Static Pages", Roles.SystemAdminRole),
                new PageInfoDTO("/app/static/pages/edit/:id", "Edit Static Page", Roles.SystemAdminRole),

                //Membership
                new PageInfoDTO("/app/users", "Users", Roles.SystemAdminRole),
                new PageInfoDTO("/app/users/edit/:id", "Edit User", Roles.SystemAdminRole),
                new PageInfoDTO("/app/roles", "Roles", Roles.SuperAdminRole),
                new PageInfoDTO("/app/companies", "Companies", Roles.SystemAdminRole),
                new PageInfoDTO("/app/companies/edit/:id", "Company Details", Roles.SystemAdminRole),
                new PageInfoDTO("/app/allowed-ip/edit/:id", "Allowed Ip", new List<string> {Roles.SystemAdminRole, Roles.SuperAdminRole}),

                new PageInfoDTO("#theme", "Themes", Roles.SystemAdminRole),

                new PageInfoDTO("/app/document", "Documents", new List<string> {Roles.SystemAdminRole, Roles.SuperAdminRole}),
                new PageInfoDTO("/app/menu-setup", "Menu Setup", new List<string> {Roles.SystemAdminRole, Roles.SuperAdminRole}),

                new PageInfoDTO("/app/resource", "Resource", new List<string> {Roles.SystemAdminRole, Roles.SuperAdminRole}),
                new PageInfoDTO("/app/section-zero", "Section Zero", new List<string> {Roles.SystemAdminRole, Roles.SuperAdminRole}),

                new PageInfoDTO("/app/project/item", "Project Item", Roles.SystemAdminRole),

                #region Setup
                new PageInfoDTO("/app/setup/status-option", "Status Option", new List<string> {Roles.SystemAdminRole, Roles.SuperAdminRole}),
                new PageInfoDTO("/app/setup/system-label", "System Label", new List<string> {Roles.SystemAdminRole, Roles.SuperAdminRole}),
                new PageInfoDTO("/app/setup/system-option", "System Option", new List<string> {Roles.SystemAdminRole, Roles.SuperAdminRole}),
                new PageInfoDTO("/app/setup/change-request-authority", "Change Request Authority", new List<string> {Roles.SystemAdminRole, Roles.SuperAdminRole}),
                new PageInfoDTO("/app/setup/change-request-status", "Change Request Status", new List<string> {Roles.SystemAdminRole, Roles.SuperAdminRole}),
                #endregion

                #region main-menu
                new PageInfoDTO("/app/menu", "Menu", Roles.SuperAdminRole),
                new PageInfoDTO("/app/projects", "Projects", new List<string> {Roles.SystemAdminRole, Roles.SuperAdminRole}),
                new PageInfoDTO("/app/user-info", "User Info", new List<string> {Roles.SystemAdminRole, Roles.SuperAdminRole}),
                #endregion

                #region projects
                //new PageInfoDTO("/app/projects/project-settings/:id", "Projects settings",new List<string> {Roles.SystemAdminRole, Roles.SuperAdminRole}),
                //new PageInfoDTO("/app/projects/project-settings/board-and-team/:id", "Board and Team", new List<string> {Roles.SystemAdminRole, Roles.SuperAdminRole}),
                //new PageInfoDTO("/app/projects/project-settings/details/:id", "Project Details", new List<string> {Roles.SystemAdminRole, Roles.SuperAdminRole}),
                //new PageInfoDTO("/app/projects/project-settings/documents/:id", "Project Documents", new List<string> {Roles.SystemAdminRole, Roles.SuperAdminRole}),
                //new PageInfoDTO("/app/projects/project-settings/estimate-items/:id", "Project Estimate Items", new List<string> {Roles.SystemAdminRole, Roles.SuperAdminRole}),
                //new PageInfoDTO("/app/projects/project-settings/estimate-summary/:id", "Project Estimate Summary", new List<string> {Roles.SystemAdminRole, Roles.SuperAdminRole}),
                //new PageInfoDTO("/app/projects/project-settings/remarks/:id", "Project Remarks", new List<string> {Roles.SystemAdminRole, Roles.SuperAdminRole}),
                //new PageInfoDTO("/app/projects/project-settings/section-zero/:id", "Section Zero", new List<string> {Roles.SystemAdminRole, Roles.SuperAdminRole}),
                //new PageInfoDTO("/app/projects/project-settings/summary/:id", "Project Summary", new List<string> {Roles.SystemAdminRole, Roles.SuperAdminRole}),
                //new PageInfoDTO("/app/projects/project-settings/estimate-items/:id", "Project Item", new List<string> {Roles.SystemAdminRole, Roles.SuperAdminRole}),
                //new PageInfoDTO("/app/projects/project-settings/estimate-items/estimate-build/:id", "Estimate Build", new List<string> {Roles.SystemAdminRole, Roles.SuperAdminRole}),
                #endregion

                new PageInfoDTO("/app/departments", "Departments", Roles.SystemAdminRole),
                new PageInfoDTO("/app/departments-team", "Departments Team", Roles.SystemAdminRole),
                new PageInfoDTO("/app/sites", "Sites", Roles.SystemAdminRole),
                new PageInfoDTO("/app/projects/project-settings", "Project Settings", Roles.SystemAdminRole),
                new PageInfoDTO("/app/suppliers", "Suppliers", Roles.SystemAdminRole),
                new PageInfoDTO("/app/customer", "Customers", new List<string> {Roles.SystemAdminRole, Roles.SuperAdminRole}),
               
            };

            #region Link modules routes
            var linkers = ModuleLinker.GetInstances<IRouteRolesModuleLinkage>();
            linkers.ForEach(o => result.AddRange(o.GetRouteRoles(serviceScope)));
            #endregion

            _routeRolesService.SetRouteRoles(result.ToArray());
        }
    }
}