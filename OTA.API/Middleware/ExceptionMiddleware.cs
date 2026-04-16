using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using OTA.API.Helpers;
using Serilog;

namespace OTA.API.Middleware
{
    /// <summary>
    /// Global exception-handling middleware that intercepts all unhandled exceptions thrown
    /// during request processing, logs them with Serilog, and returns a standardised JSON
    /// error response using <see cref="ApiResponse{T}"/>.
    /// </summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var traceId = context.TraceIdentifier;

            int statusCode;
            string message;
            List<string> errors = new();

            switch (exception)
            {
                case ValidationException validationEx:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    message = "One or more validation errors occurred.";
                    errors.Add(validationEx.Message);
                    _logger.LogWarning(validationEx,
                        "Validation error on {Method} {Path} | TraceId: {TraceId}",
                        context.Request.Method, context.Request.Path, traceId);
                    break;

                case ArgumentException argEx:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    message = argEx.Message;
                    _logger.LogWarning(argEx,
                        "Argument error on {Method} {Path} | TraceId: {TraceId}",
                        context.Request.Method, context.Request.Path, traceId);
                    break;

                case UnauthorizedAccessException unauthorizedEx:
                    statusCode = (int)HttpStatusCode.Unauthorized;
                    message = unauthorizedEx.Message;
                    _logger.LogWarning(unauthorizedEx,
                        "Unauthorized access on {Method} {Path} | TraceId: {TraceId}",
                        context.Request.Method, context.Request.Path, traceId);
                    break;

                case KeyNotFoundException notFoundEx:
                    statusCode = (int)HttpStatusCode.NotFound;
                    message = notFoundEx.Message;
                    _logger.LogWarning(notFoundEx,
                        "Resource not found on {Method} {Path} | TraceId: {TraceId}",
                        context.Request.Method, context.Request.Path, traceId);
                    break;

                case InvalidOperationException invalidOpEx when
                    invalidOpEx.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
                    invalidOpEx.Message.Contains("conflict", StringComparison.OrdinalIgnoreCase) ||
                    invalidOpEx.Message.Contains("already registered", StringComparison.OrdinalIgnoreCase) ||
                    invalidOpEx.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase):
                    statusCode = (int)HttpStatusCode.Conflict;
                    message = invalidOpEx.Message;
                    _logger.LogWarning(invalidOpEx,
                        "Conflict error on {Method} {Path} | TraceId: {TraceId}",
                        context.Request.Method, context.Request.Path, traceId);
                    break;

                case OperationCanceledException:
                    statusCode = 499;
                    message = "The request was cancelled by the client.";
                    _logger.LogInformation(
                        "Request cancelled on {Method} {Path} | TraceId: {TraceId}",
                        context.Request.Method, context.Request.Path, traceId);
                    break;

                default:
                    statusCode = (int)HttpStatusCode.InternalServerError;
                    message = "An unexpected error occurred. Please try again later.";
                    _logger.LogError(exception,
                        "Unhandled exception on {Method} {Path} | TraceId: {TraceId}",
                        context.Request.Method, context.Request.Path, traceId);
                    break;
            }

            var response = new
            {
                success = false,
                message,
                statusCode,
                traceId,
                errors = errors.Count > 0 ? errors : null,
                data = (object?)null
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var json = JsonSerializer.Serialize(response, JsonOptions);
            await context.Response.WriteAsync(json);
        }
    }

    /// <summary>
    /// Extension methods to register <see cref="ExceptionMiddleware"/> in the pipeline.
    /// </summary>
    public static class ExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
            => app.UseMiddleware<ExceptionMiddleware>();
    }
}
