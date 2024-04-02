using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Core.Membership.Services;
using ClaimTypes = System.Security.Claims.ClaimTypes;
using ControllerBase = Core.Web.ControllerBase;

namespace Core.Membership.Api
{
    [Produces("application/json")]
    [Route("api/route")]
    public class RouteController : ControllerBase
    {
        private readonly IRouteRolesService _routesService;
        private readonly IUserService _userService;

        public RouteController(
            IUserService userService,
            IRouteRolesService routesService,
            ILogger<ControllerBase> logger) : base(logger)
        {
            _routesService = routesService;
            _userService = userService;
        }

        [HttpGet, Route("routes-roles")]
        [Authorize(Roles = Roles.SuperAdminRole + "," + Roles.SystemAdminRole)]
        public IActionResult GetRoutesRoles() =>
            Ok(_routesService.GetRouteRoles());

        [HttpGet, Route("pages-roles")]
        [Authorize(Roles = Roles.SuperAdminRole + "," + Roles.SystemAdminRole)]
        public async Task<IActionResult> GetPageRoles(CancellationToken cancellationToken = default) =>
            Ok(await _routesService.GetPagesRoles(cancellationToken));

        [HttpGet, Route("me")]
        public async Task<IActionResult> GetPageRoutesPathsForRolesAndGroups(CancellationToken cancellationToken = default) =>
            Ok(await _routesService.GetPageRoutesPathsForUser(User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken));
    }
}