using Core.Membership;
using Core.Membership.Enums;
using Core.Membership.Utils;
using System.Linq;

namespace Project.Services
{
    public class ApiAccessModelGetter : IApiAccessModelGetter
    {
        public ApiAccessModel GetApiAccessModel() =>
            PermissionsExtractor.GetPermissionNamesOfClass(typeof(Permissions)).Any() ?
            ApiAccessModel.PermissionBased :
            ApiAccessModel.RoleBased;
    }
}