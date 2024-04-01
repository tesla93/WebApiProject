using System;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;

namespace BBWM.Core.Web.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiVersionAttribute : Attribute, IActionFilter
    {
        private readonly IWebHostEnvironment _hostingEnvironment;

        public ApiVersionAttribute(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            var request = context.HttpContext.Request;
            if (!request.Path.StartsWithSegments("/api") && request.Method == "GET") return;

            if (request.Path.Value.Contains("printOrderManifest") || request.Path.Value.Contains("printConsignmentManifest"))
            {
                return;
            }

            var version = Assembly.GetEntryAssembly().GetName().Version?.ToString() ?? "1.0.0.0";

            if (_hostingEnvironment.IsDevelopment())
            {
                version = "develop";
            }

            try
            {
                context.HttpContext.Response.Headers["api-version"] = version;
            }
            catch (Exception)
            { }
        }
    }
}