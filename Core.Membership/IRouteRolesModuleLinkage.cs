using Core.Membership.DTO;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Core.Membership
{
    public interface IRouteRolesModuleLinkage
    {
        List<PageInfoDTO> GetRouteRoles(IServiceScope serviceScope);
    }
}
