using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace BBWM.Core.Web.Middlewares
{
    public class AntiforgeryTokenMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAntiforgery _antiforgery;

        public AntiforgeryTokenMiddleware(RequestDelegate next, IAntiforgery antiforgery)
        {
            _next = next;
            _antiforgery = antiforgery;
        }

        public Task Invoke(HttpContext context)
        {
            var paths = new[] { "/", "/app", "/account", "/api" };

            if (paths.Any(o => context.Request.Path.StartsWithSegments(o, StringComparison.OrdinalIgnoreCase)))
            {
                var tokens = _antiforgery.GetAndStoreTokens(context);
                context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken, new CookieOptions { HttpOnly = false });
            }

            return _next(context);
        }
    }

    public static class AntiforgeryTokenMiddlewareExtensions
    {
        public static IApplicationBuilder UseAntiforgeryToken(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AntiforgeryTokenMiddleware>();
        }
    }
}