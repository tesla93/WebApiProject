using Microsoft.AspNetCore.Builder;

namespace BBWM.Core.Web.Middlewares
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseHttpToHttpsRedirectMiddleware(this IApplicationBuilder builder) =>
            builder.UseMiddleware<HttpToHttpsRedirectMiddleware>();

        public static IApplicationBuilder UseAddUserToLogsMiddleware(this IApplicationBuilder builder) =>
            builder
                .UseMiddleware<AddUsernameToLogsMiddleware>()
                .UseMiddleware<AddUserImpersonationToLogsMiddleware>();

        public static IApplicationBuilder UseErrorHandlingMiddleware(this IApplicationBuilder builder) =>
            builder.UseMiddleware<ErrorHandlingMiddleware>();
    }
}