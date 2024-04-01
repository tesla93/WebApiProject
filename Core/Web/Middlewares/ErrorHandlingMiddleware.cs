using BBWM.Core.Exceptions;
using BBWM.ReportProblem;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BBWM.Core.Web.Middlewares
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IReportProblemService _reportProblemService;

        public static readonly BlockingCollection<Exception> HandlerExceptionsDebug = new BlockingCollection<Exception>();

        public ErrorHandlingMiddleware(RequestDelegate next,
            ILogger<ErrorHandlingMiddleware> logger,
            IWebHostEnvironment hostingEnvironment,
            IReportProblemService reportProblemService)
        {
            this.next = next;
            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
            _reportProblemService = reportProblemService;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                HandlerExceptionsDebug.Add(ex);
                if (HandlerExceptionsDebug.Count > 10) HandlerExceptionsDebug.Take();

                await HandleException(context, ex);
            }
        }

        private async Task HandleException(HttpContext context, Exception ex)
        {
            var isDevelopment = _hostingEnvironment.IsDevelopment();

            var internalServerErrorMessage = $"Internal Server Error (Error ID: {Guid.NewGuid()})";
            _logger.LogError(ex, internalServerErrorMessage);

            int code;
            string message = ex.Message;

            switch (ex)
            {
                case ActionNotImplementedException _: code = StatusCodes.Status501NotImplemented; break;
                case ForbiddenException _: code = StatusCodes.Status403Forbidden; break;
                case EntityNotFoundException _: code = StatusCodes.Status404NotFound; break;
                case ConflictException _: code = StatusCodes.Status409Conflict; break;
                case ApiException _: code = StatusCodes.Status400BadRequest; break;
                case BusinessException _: code = StatusCodes.Status400BadRequest;  break;
                case ValidationException validationException:
                    code = StatusCodes.Status400BadRequest;
                    message = FormatValidationException(validationException);
                    break;

                default:
                    if (!isDevelopment)
                    {
                        try
                        {
                            await _reportProblemService.AutoSend(ex);
                        }
                        catch
                        {
                            _logger.LogError(ex, "An error occurred while sending an exception report.");
                        }
                    }
                    code = StatusCodes.Status500InternalServerError;
                    message = isDevelopment ? ex.Message : internalServerErrorMessage;
                    break;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = code;
            await context.Response.WriteAsync(message);
        }

        private string FormatValidationException(ValidationException ex)
        {
            ValidationProblemDetails GetValidationResult(ValidationException e)
            {
                if (e.ValidationResult.MemberNames == null || !e.ValidationResult.MemberNames.Any())
                {
                    return new ValidationProblemDetails(
                        new Dictionary<string, string[]> { { "DTO", new[] { e.ValidationResult.ErrorMessage } } });
                }
                return new ValidationProblemDetails(
                    e.ValidationResult.MemberNames.ToDictionary(key => key, value => new[] { e.ValidationResult.ErrorMessage }));
            }
            return JsonSerializer.Serialize(GetValidationResult(ex));
        }
    }
}
