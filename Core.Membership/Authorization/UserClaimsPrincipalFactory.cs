using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Core.Membership.Model;
using Core.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Core.Membership.Authorization
{
    public class UserClaimsPrincipalFactory : UserClaimsPrincipalFactory<User, Role>
    {
        public UserClaimsPrincipalFactory(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor)
        {}


        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
        {
            var id =  await base.GenerateClaimsAsync(user);

            // Company claim
            if (user.CompanyId.HasValue)
                id.AddClaim(new Claim(MultiTenancyService.TenantField, user.CompanyId.ToString()));

            // Permissions claims
            user = await UserManager.Users
                .Include(x => x.UserPermissions)
                .ThenInclude(x => x.Permission)
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .ThenInclude(x => x.RolePermissions)
                .ThenInclude(x => x.Permission)
                .SingleOrDefaultAsync(x => x.Id == user.Id);
            var userPermissionNames = user.UserPermissions.Select(x => x.Permission.Name)
                .Union(user.UserRoles.Select(x => x.Role).SelectMany(x => x.RolePermissions).Select(x => x.Permission.Name))
                .Distinct();
            id.AddClaims(userPermissionNames.Select(x => new Claim(x, "true")));

            return id;
        }
    }
}
