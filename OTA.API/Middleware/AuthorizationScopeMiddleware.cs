using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace OTA.API.Middleware
{
    /// <summary>
    /// Middleware that enforces customer and project scope constraints for the CustomerAdmin role.
    /// When an authenticated CustomerAdmin accesses a resource that contains a customerId or
    /// projectId in the route values or query string, this middleware verifies that the
    /// requested identifiers match the CustomerId and ProjectScope claims embedded in their JWT.
    ///
    /// If a scope violation is detected the request is terminated immediately with HTTP 403 Forbidden.
    /// Platform-level roles (SuperAdmin, PlatformAdmin, etc.) bypass this check entirely.
    /// </summary>
    public class AuthorizationScopeMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthorizationScopeMiddleware> _logger;

        private const string CustomerAdminRole = "CustomerAdmin";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public AuthorizationScopeMiddleware(RequestDelegate next, ILogger<AuthorizationScopeMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only apply scope enforcement to authenticated CustomerAdmin users.
            if (context.User.Identity?.IsAuthenticated != true)
            {
                await _next(context);
                return;
            }

            var role = context.User.FindFirstValue(ClaimTypes.Role)
                ?? context.User.FindFirstValue("role");

            if (!string.Equals(role, CustomerAdminRole, StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            // Extract the CustomerAdmin's own customerId from JWT claims.
            var claimsCustomerId = context.User.FindFirstValue("customerId");

            // Extract project scope (comma-separated or repeated claims).
            var projectScopeClaim = context.User.FindFirstValue("projectScope");
            var allowedProjectIds = string.IsNullOrEmpty(projectScopeClaim)
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(
                    projectScopeClaim.Split(',', StringSplitOptions.RemoveEmptyEntries),
                    StringComparer.OrdinalIgnoreCase);

            // ── Check customerId in route or query ────────────────────────────
            var requestedCustomerId = GetRouteOrQueryValue(context, "customerId");

            if (!string.IsNullOrEmpty(requestedCustomerId)
                && !string.Equals(requestedCustomerId, claimsCustomerId, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "CustomerAdmin {UserId} attempted to access customerId '{RequestedId}' but is scoped to '{ClaimedId}'. Path: {Path}",
                    context.User.FindFirstValue("userId"),
                    requestedCustomerId,
                    claimsCustomerId,
                    context.Request.Path);

                await WriteForbiddenResponseAsync(context, "Access denied: you do not have permission to access resources belonging to the requested customer.");
                return;
            }

            // ── Check projectId in route or query ─────────────────────────────
            // Only enforce when the CustomerAdmin has an explicit non-empty project scope.
            // An empty scope list means the CustomerAdmin is allowed all projects within their customer.
            if (allowedProjectIds.Count > 0)
            {
                var requestedProjectId = GetRouteOrQueryValue(context, "projectId");

                if (!string.IsNullOrEmpty(requestedProjectId)
                    && !allowedProjectIds.Contains(requestedProjectId))
                {
                    _logger.LogWarning(
                        "CustomerAdmin {UserId} attempted to access projectId '{RequestedProjectId}' outside their project scope. Path: {Path}",
                        context.User.FindFirstValue("userId"),
                        requestedProjectId,
                        context.Request.Path);

                    await WriteForbiddenResponseAsync(context, "Access denied: you do not have permission to access the requested project.");
                    return;
                }
            }

            await _next(context);
        }

        /// <summary>
        /// Resolves a named parameter by first checking route values, then the query string.
        /// Route values take precedence over query string parameters.
        /// </summary>
        private static string? GetRouteOrQueryValue(HttpContext context, string key)
        {
            if (context.Request.RouteValues.TryGetValue(key, out var routeVal)
                && routeVal is string routeStr
                && !string.IsNullOrEmpty(routeStr))
            {
                return routeStr;
            }

            if (context.Request.Query.TryGetValue(key, out var queryVal)
                && !string.IsNullOrEmpty(queryVal))
            {
                return queryVal.ToString();
            }

            return null;
        }

        private static async Task WriteForbiddenResponseAsync(HttpContext context, string message)
        {
            var response = new
            {
                success = false,
                message,
                statusCode = (int)HttpStatusCode.Forbidden,
                traceId = context.TraceIdentifier,
                errors = (object?)null,
                data = (object?)null
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
    }

    public static class AuthorizationScopeMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomerScopeEnforcement(this IApplicationBuilder app)
            => app.UseMiddleware<AuthorizationScopeMiddleware>();
    }
}
