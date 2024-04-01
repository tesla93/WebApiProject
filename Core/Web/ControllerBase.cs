using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Core.DTO;
using Core.ModelHashing;
using Microsoft.Extensions.Logging;

namespace Core.Web
{
    /// <summary>
    /// Represents the class of base controller
    /// </summary>
    public abstract class ControllerBase : Controller
    {
        protected readonly ILogger<ControllerBase> Logger;

        protected ControllerBase(ILogger<ControllerBase> logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Returns the URL consisting of the current domain and the specified tail.
        /// </summary>
        /// <param name="tail">The tail which should be added to the end of the URL.</param>
        protected string CalculateUrl(string tail) =>
            Request.Scheme + "://" + Request.Host + "/" + tail;

        protected async Task<IActionResult> NoContent(Func<Task> action)
        {
            await action();
            return NoContent();
        }

        protected async Task<IActionResult> GetOkResult<T>(Func<Task<T>> action)
        {
            return Ok(await action());
        }

        protected CreatedResult Created<TEntityDTO, TKey>(TEntityDTO result, IModelHashingService modelHashingService) where TEntityDTO: IDTO<TKey> where TKey: IEquatable<TKey> 
        {
            var idStringValue = Convert.ToString(result.Id);
            var hashedId = modelHashingService.HashProperty(result, nameof(IDTO<TKey>.Id));
            return Created(
                $"{Request.Scheme}://{Request.Host.Value}{Request.Path}/{hashedId ?? idStringValue}",
                result);
        }
    }
}