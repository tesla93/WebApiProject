using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Core.Web;
using Core.Membership;
using Core.Crud;

namespace Core.Audit
{
    [Route("api/data-audit")]
    [Authorize(Roles = Roles.SystemAdminRole)]
    public class DataAuditController: PagedDataControllerBase<ChangeLogDTO>
    {
        public DataAuditController(
            IDataAuditService service,
            ILogger<DataAuditController> logger) : base(service, logger)
        {
        }
    }
}