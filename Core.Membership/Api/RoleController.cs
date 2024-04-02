using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Core.Membership.DTO;
using Core.Membership.Services;
using Core.Web;
using Core.Web.Filters;
using Core.Filters;
using Core.Crud;

namespace Core.Membership.Api
{
    [Route("api/role")]
    [ReadWriteAuthorize(ReadRoles = Roles.SystemAdminRole + "," + Roles.SuperAdminRole, WriteRoles = Roles.SuperAdminRole)]
    public class RoleController : PagedCrudControllerBase<RoleDTO, string>
    {
        private readonly IApiAccessModelGetter _apiAccessModelGetter;

        private IRoleService RoleService => PagedCrudService as IRoleService;

        public RoleController(
            IRoleService roleService,
            IApiAccessModelGetter apiSecurityModelGetter,
            ILogger<RoleController> logger)
            : base(roleService, logger)
        {
            _apiAccessModelGetter = apiSecurityModelGetter;
        }

        protected override async Task AfterWriteCrudAction(CancellationToken cancellationToken = default)
        {
            // This code is reposnible for updating roles metadata and git data. Only changes that come from
            // the Manager Roles page and go through this controller trigger updating. It's done for simplicity because
            // for now we don't expect any other entry point which could change roles.
            // TODO: then it should become a part of new GitData (ref GitLab, Metadata) 
            //await _roleGitDataService.SendToGit(cancellationToken);
        }

        [HttpGet]
        [Route("core")]
        public IActionResult GetCoreRoles(CancellationToken cancellationToken = default) =>
            Ok(RoleService.GetHardcodedRoles(cancellationToken));

        [HttpGet]
        [Route("model")]
        [AllowAnonymous]
        public IActionResult GetApiAccessModel() =>
            Ok(_apiAccessModelGetter.GetApiAccessModel());
    }
}
