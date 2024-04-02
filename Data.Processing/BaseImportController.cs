using AutofacExtensions;
using BackgroundProcessing;
using Core.Services;
using DataProcessing.Classes;
using DataProcessing.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace DataProcessing
{
    public abstract class BaseImportController : Controller
    {
        private readonly IBackgroundTaskQueue _queue;

        private readonly ICurrentUserService _currentUserService;

        private readonly IImportService _importService;

        public BaseImportController(
            IBackgroundTaskQueue queue,
            ICurrentUserService currentUserService,
            IImportService importService)
        {
            _queue = queue;
            _importService = importService;
            _currentUserService = currentUserService;
        }

        [HttpPost, Route("[action]"), IgnoreLogging]
        public virtual async Task<IActionResult> Import(ImportDataModel model)
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    await model.File.CopyToAsync(stream);
                    return Ok(await _importService.Import(_importService.CreateSettings(model, stream, _currentUserService.GetCurrentUserId()), default));
                }
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.Forbidden, ex.Message);
            }
        }
    }
}
