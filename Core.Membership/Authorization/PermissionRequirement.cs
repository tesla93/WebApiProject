using Microsoft.AspNetCore.Authorization;

namespace Core.Membership.Authorization
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }

        public PermissionRequirement(string permission) => Permission = permission;
    }
}
