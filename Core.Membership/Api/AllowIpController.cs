using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Core.Membership.DTO;
using Core.Web;
using Core.Crud;
using Core.Crud.Interfaces;

namespace Core.Membership.Api
{
    [Route("api/allowed-ip")]
    [Authorize(Roles = Roles.SystemAdminRole + "," + Roles.SuperAdminRole)]
    public class AllowIpController : PagedCrudControllerBase<AllowedIpDTO>
    {
        public AllowIpController(
            IPagedCrudService<AllowedIpDTO> service,
            ILogger<AllowIpController> logger) : base(service, logger)
        {
        }
    }
}