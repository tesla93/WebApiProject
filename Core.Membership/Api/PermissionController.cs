using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Core.Membership.Services;
using ControllerBase = Core.Web.ControllerBase;

namespace Core.Membership.Api
{
    [Route("api/permission")]
    [Authorize(Roles = Roles.SystemAdminRole + "," + Roles.SuperAdminRole)]
    public class PermissionController : ControllerBase
    {
        private readonly IPermissionService _permissionService;

        public PermissionController(
            ILogger<ControllerBase> logger,
            IPermissionService permissionService) : base(logger) =>
            _permissionService = permissionService;

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default) =>
            Ok(await _permissionService.GetAll(cancellationToken));
    }
}
