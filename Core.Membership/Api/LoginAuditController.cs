using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Core.Membership.DTO;
using Core.Membership.Services;
using Core.Web;
using Core.Crud;

namespace Core.Membership.Api
{
    [Route("api/login-audit")]
    [Authorize(Roles = Roles.SystemAdminRole)]
    public class LoginAuditController : PagedDataControllerBase<LoginAuditDTO>
    {
        public LoginAuditController(
            ILoginAuditService auditService,
            ILogger<LoginAuditController> logger) : base(auditService, logger)
        {
        }

        [HttpGet, Route("{id}"), ResponseCache(NoStore = true)]
        public override Task<IActionResult> Get(int id, CancellationToken cancellationToken = default) =>
            base.Get(id, cancellationToken);
    }
}