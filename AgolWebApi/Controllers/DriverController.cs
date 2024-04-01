using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Data.DTO;

namespace BBWT.Server.Api
{
    [Produces("application/json")]
    [Route("api/order")]
    //[ReadWriteAuthorize(ReadRoles = Roles.MfdAdminStaff + "," + Roles.SystemAdminRole + "," + Roles.SuperAdminRole + "," + Roles.Customer + "," + Roles.DriverStaff, WriteRoles = Roles.DriverStaff + "," + Roles.SystemAdminRole)]
    public class DriverController : PagedCrudControllerBase<OrderDTO>
    {
        //private readonly IDriverService _driverService;
        //IFileStorageService _fileStorageService;


        public DriverController(
            //IDriverService driverService,
            //IFileStorageService fileStorageService,
            ILogger<DriverController> logger) : base( logger)
        {
            //this._driverService = driverService;
            //this._fileStorageService = fileStorageService;
        }

        //[HttpGet]
        //public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default) =>
        //   Ok(await this.CrudService.GetAll(cancellationToken));

        //[HttpPost, Route("upload-attachment")]
        //public async Task<IActionResult> UploadAttachment(IFormCollection formData, CancellationToken cancellationToken)
        //{
        //    var files = Request.Form.Files;
        //    if (files == null) return BadRequest("There is no uploaded file.");

        //    // Binding files to user and operation name for removing old files
        //    var additionalData =
        //        formData.Keys.ToDictionary<string, string, string>(key => key, key => formData[key]);
        //    additionalData.Add("thumbnail_size", "400");
        //    additionalData.Add("operation_name", "FileUploading");

        //    var uploadResult = (await _fileStorageService.UploadFiles(files.ToArray(), additionalData, cancellationToken)).SuccessfullyUploadedFiles;
        //    return Ok(uploadResult);
        //}

        //[HttpGet]
        //[Route("get-assigned-orders")]
        //[Authorize(Roles = Roles.SystemAdminRole + "," + Roles.DriverStaff)]
        //public async Task<IActionResult> GetAssignedOrders( CancellationToken cancellationToken=default)
        //{
        //    return Ok(await _driverService.GetAssignedOrders(cancellationToken));
        //}
    }
}