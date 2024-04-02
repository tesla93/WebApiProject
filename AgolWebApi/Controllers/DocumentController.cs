using Core.Crud;
using Core.ModelHashing;
using Core.Web;
using Core.Web.ModelBinders;
using Project.Data.DTO;
using Project.Services.Document;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Api
{
    [Produces("application/json")]
    [Route("api/document")]
    [Authorize]
    public class DocumentController : PagedCrudControllerBase<DocumentDTO>
    {
        private readonly IConfiguration config;
        private readonly IDocumentService _DocumentService;
        private readonly IHttpContextAccessor _contextAccessor;

        public DocumentController(
            IDocumentService DocumentService,
            IHttpContextAccessor contextAccessor,
            IConfiguration config,
            ILogger<DocumentController> logger) : base(DocumentService, logger)
        {
            this.config = config;
            _DocumentService = DocumentService;
            _contextAccessor = contextAccessor;
        }

        public override Task<IActionResult> Create([FromBody] DocumentDTO dto, [FromServices] IModelHashingService modelHashingService, CancellationToken cancellationToken = default)
        {
            var downloadFolder = config.GetSection("DownloadFolder").Value;
            string newName = Guid.NewGuid() + Path.GetExtension(dto.Name);
            dto.Path = newName;

            Directory.CreateDirectory(downloadFolder);

            System.IO.File.WriteAllBytes(Path.Combine(downloadFolder, newName), Convert.FromBase64String(dto.Base64.Split(',')[1]));
            return base.Create(dto, modelHashingService, cancellationToken);
        }

        public override async Task<IActionResult> Delete([IdBinder] int id, CancellationToken cancellationToken = default)
        {
            var downloadFolder = config.GetSection("DownloadFolder").Value;

            var document = await _DocumentService.Get(id, cancellationToken);

            if(System.IO.File.Exists(Path.Combine(downloadFolder, document.Path)))
            {
                System.IO.File.Delete(Path.Combine(downloadFolder, document.Path));
            }

            return await base.Delete(id, cancellationToken);
        }

        [HttpGet("Download/{path}")]
        public IActionResult Download(string path)
        {
            var downloadFolder = config.GetSection("DownloadFolder").Value;

            if (System.IO.File.Exists(Path.Combine(downloadFolder, path)))
            {
                byte[] byteArray = System.IO.File.ReadAllBytes(Path.Combine(downloadFolder, path));

                return new FileContentResult(byteArray, "application/octet-stream");
            }

            return NotFound();
        }
    }
}