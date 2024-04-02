using BackgroundProcessing;
using Core.Services;
using DataProcessing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using DataProcessing.Classes;
using Core.Web;
using Core.Crud;
using Core.Membership;
using Core.Web.Filters;
using Project.Data.DTO;
using Microsoft.Extensions.Logging;
using System.Threading;
using ControllerBase = Microsoft.AspNetCore.Mvc.ControllerBase;
using System.IO;

namespace Server.Api
{
    [ReadWriteAuthorize(ReadRoles = ReadWriteAuthorizeAttribute.Authenticated, WriteRoles = Roles.SuperAdminRole + "," + Roles.SystemAdminRole)]
    [Produces("application/json")]
    [Route("api/data-export")]
    public class ProjectDataExportController: ControllerBase
    {
        //private IProjectDataExportService _projectDataExportService;
        private ICurrentUserService _currentUserService;

        public ProjectDataExportController(
            ICurrentUserService currentUserService
            //IProjectDataExportService projectDataExportService
            )
        {
            //_projectDataExportService = projectDataExportService;
            _currentUserService = currentUserService;

        }

        //[HttpGet, Route("xlstransfer/{id}")]
        //public async Task<FileResult> XLSTransfer(int id, CancellationToken cancellationToken = default)
        //{
        //    var res = await _projectDataExportService.XlsTransfer(id, cancellationToken);
        //    var file = File(res, "application/octet-stream");
        //    return file;
        //}
    }
}