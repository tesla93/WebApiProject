using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.EntityFrameworkCore;
using Core.Membership.DTO;
using Core.Membership.Enums;
using Core.Membership.Exceptions;
using Core.Membership.Model;
using Core.Membership.Utils;
using Core.Web.Filters;

namespace Core.Membership.Services
{
    public class RouteRolesService : IRouteRolesService
    {
        private static PageInfoDTO[] _routeInfo = { };

        private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
        private readonly IUserService _userService;
        private readonly UserManager<User> _userManager;


        public RouteRolesService(
            IActionDescriptorCollectionProvider actionDescriptorCollectionProvider,
            //IStaticPageService staticPageService,
            IUserService userService,
            UserManager<User> userManager)
        {
            _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
            //_staticPageService = staticPageService;
            _userService = userService;
            _userManager = userManager;
        }


        public IEnumerable<ApiEndPointInfoDTO> GetRouteRoles()
        {
            return _actionDescriptorCollectionProvider.ActionDescriptors.Items.OfType<ControllerActionDescriptor>().Select(descriptor =>
            {
                // Path
                var path = descriptor.AttributeRouteInfo?.Template ??
                    $"{descriptor.RouteValues["controller"]}/{descriptor.RouteValues["action"]}".ToLowerInvariant();

                // Method
                var method = string.Join(", ",
                    descriptor.ActionConstraints?
                        .OfType<HttpMethodActionConstraint>()
                        .FirstOrDefault()?
                        .HttpMethods
                    ?? new [] { "GET" });

                // Roles
                var roles = new List<string>();

                var authorizeAttribute = descriptor.EndpointMetadata.OfType<AuthorizeAttribute>().ToList();
                if (descriptor.FilterDescriptors.Any(fd => fd.Filter is AllowAnonymousFilter) || !descriptor.FilterDescriptors.Any(fd => fd.Filter is AuthorizeFilter))
                {
                    roles.Add(AggregatedRole.Anyone);
                }
                else
                {
                    var readWriteAuthorizeAttribute = descriptor.EndpointMetadata.OfType<ReadWriteAuthorizeAttribute>().SingleOrDefault();
                    var list = new List<string>();
                    if (authorizeAttribute.Any())
                    {
                        list = authorizeAttribute
                            .Where(attributeItem => attributeItem.Roles != null)
                            .SelectMany(attributeItem => attributeItem.Roles.Split(","))
                            .Select(roleItem => roleItem.Trim())
                            .ToList();
                    }
                    else
                    {
                        if (readWriteAuthorizeAttribute != null)
                        {
                            var readWriteRoles = readWriteAuthorizeAttribute.ReadWriteRoles;
                            if (readWriteRoles == null)
                            {
                                switch (method)
                                {
                                    case "GET":
                                        readWriteRoles = readWriteAuthorizeAttribute.ReadRoles;
                                        break;
                                    case "POST":
                                    case "PUT":
                                    case "PATCH":
                                    case "DELETE":
                                        readWriteRoles = readWriteAuthorizeAttribute.WriteRoles;
                                        break;
                                }
                            }

                            if (readWriteRoles != null)
                            {
                                list = readWriteRoles.Split(", ").Select(roleItem => roleItem.Trim()).ToList();
                            }
                        }
                    }

                    if (list.Any())
                    {
                        roles.AddRange(list);
                    }
                    if (roles.Count == 0)
                    {
                        roles.Add(AggregatedRole.Authenticated);
                    }
                }

                // Permissions
                var assemblyPermissionNames = PermissionsExtractor.GetAllPermissionNamesOfAssembly(descriptor.ControllerTypeInfo.Assembly);
                var permissions = authorizeAttribute
                    .Where(x => assemblyPermissionNames.Contains(x.Policy))
                    .Select(x => x.Policy)
                    .ToList();

                var dto = new ApiEndPointInfoDTO
                {
                    Method = method,
                    Path = path,
                    Roles = roles.Distinct().ToList(),
                    Permissions = permissions
                };

                return dto;
            }).OrderBy(a => a.Path).ThenBy(a => a.Method);
        }

        // So avoiding setting of a static variable from a non-static method. As well this meets SonarQube requirements.
        public void SetRouteRoles(PageInfoDTO[] data) => SetStaticRouteRoles(data);

        public async Task<IEnumerable<PageInfoDTO>> GetPagesRoles(CancellationToken cancellationToken = default)
        {
            var result = _routeInfo.ToList();
            await AddStaticPages(result, cancellationToken);
            return result;
        }

        public async Task<string[]> GetPageRoutesPathsForUser(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.Users
                .Include(x => x.UserGroups).ThenInclude(x => x.Group)
                .Include(x => x.UserRoles).ThenInclude(x => x.Role)
                .SingleOrDefaultAsync(o => o.Id == userId, cancellationToken);
                
            if (user == null) throw new UserNotExistsException();

            var allUserPermissions = await _userService.GetAllUserPermissions(userId, cancellationToken);
                
            return _routeInfo
                .Where(pageInfoItem =>
                    (HasAccessByRoles(pageInfoItem, user.UserRoles.Select(x => x.Role.Name)) ||
                     pageInfoItem.Permissions.Intersect(allUserPermissions.Select(o => o.Name)).Any())
                    &&
                    (!pageInfoItem.Groups.Any() ||
                     pageInfoItem.Groups.Intersect(user.UserGroups.Select(o => o.Group.Name)).Any()))
                .Select(roteRolesItem => roteRolesItem.Path)
                .ToArray();
        }

        private static void SetStaticRouteRoles(PageInfoDTO[] data) => _routeInfo = data;

        private static bool HasAccessByRoles(PageInfoDTO pageInfo, IEnumerable<string> userRoles) =>
            pageInfo.Roles.Contains(AggregatedRole.Anyone) ||
            pageInfo.Roles.Contains(AggregatedRole.Authenticated) ||
            pageInfo.Roles.Intersect(userRoles).Any();

        private async Task AddStaticPages(List<PageInfoDTO> roles, CancellationToken cancellationToken = default)
        {
            //foreach (var page in await _staticPageService.GetAll(cancellationToken))
            //{
            //    roles.Add(new PageInfoDTO($"/app/static/{page.Alias}", page.Heading, new List<string> { AggregatedRole.Authenticated }));
            //}
        }
    }
}