using Module.Core.Membership;
using Module.Core.Membership.DTO;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Module.DbDoc
{
    public class RouteRolesModuleLinkage : IRouteRolesModuleLinkage
    {
        public List<PageInfoDTO> GetRouteRoles(IServiceScope serviceScope) =>
            new List<PageInfoDTO>() {
                new PageInfoDTO(Routes.DbExplorer, Roles.SuperAdminRole),
                new PageInfoDTO(Routes.ColumnTypes, Roles.SuperAdminRole)
            };
    }
}
