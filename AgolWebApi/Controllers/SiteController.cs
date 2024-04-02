using Core.Crud;
using Core.Membership;
using Core.Web.Filters;
using Project.Data.DTO;
using Project.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Roles = Core.Membership.Roles;
using Project.Services.Interfaces;

namespace AgolWebApi.Controllers
{
    [Produces("application/json")]
    [Route("api/sites")]
    [ReadWriteAuthorize(ReadRoles = ReadWriteAuthorizeAttribute.Authenticated, WriteRoles = Roles.SuperAdminRole + "," + Roles.SystemAdminRole)]
    public class SiteController : PagedCrudControllerBase<SiteDTO>
    {
        private readonly ISiteService _siteService;
        private readonly IHttpContextAccessor _contextAccessor;

        public SiteController(
                   ISiteService siteService,
                   IHttpContextAccessor contextAccessor,
                   ILogger<SiteController> logger) : base(siteService, logger)
        {
            _siteService = siteService;
            _contextAccessor = contextAccessor;
        }
    }
}
