using System.Security.Claims;
using OTA.API.Helpers;

namespace OTA.API.Middleware
{
    /// <summary>
    /// Middleware that extracts the authenticated user's identity and request metadata from the
    /// HTTP context and populates the scoped <see cref="AuditContext"/> service.
    /// This makes audit information available to downstream services without them needing to
    /// take a direct dependency on <see cref="IHttpContextAccessor"/>.
    ///
    /// The middleware does NOT produce audit log records itself — that responsibility belongs
    /// to individual service methods that call IAuditLogService.
    /// </summary>
    public class AuditMiddleware
    {
        private readonly RequestDelegate _next;

        public AuditMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(HttpContext context, AuditContext auditContext)
        {
            // ── Populate user identity from JWT claims ────────────────────────
            if (context.User.Identity?.IsAuthenticated == true)
            {
                auditContext.UserId = context.User.FindFirstValue("userId")
                    ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? context.User.FindFirstValue("sub");

                auditContext.Email = context.User.FindFirstValue(ClaimTypes.Email)
                    ?? context.User.FindFirstValue("email");

                auditContext.Role = context.User.FindFirstValue(ClaimTypes.Role)
                    ?? context.User.FindFirstValue("role");
            }

            // ── Populate request metadata ─────────────────────────────────────
            auditContext.IpAddress = GetClientIpAddress(context);
            auditContext.UserAgent = context.Request.Headers.UserAgent.ToString();

            await _next(context);
        }

        /// <summary>
        /// Resolves the real client IP address by checking X-Forwarded-For first (reverse proxy),
        /// then X-Real-IP, and finally the remote endpoint address.
        /// </summary>
        private static string? GetClientIpAddress(HttpContext context)
        {
            var forwarded = context.Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrWhiteSpace(forwarded))
            {
                // X-Forwarded-For may contain a comma-separated chain; take the first (originating) address.
                var firstIp = forwarded.Split(',')[0].Trim();
                if (!string.IsNullOrEmpty(firstIp))
                    return firstIp;
            }

            var realIp = context.Request.Headers["X-Real-IP"].ToString();
            if (!string.IsNullOrWhiteSpace(realIp))
                return realIp;

            return context.Connection.RemoteIpAddress?.ToString();
        }
    }

    public static class AuditMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuditContext(this IApplicationBuilder app)
            => app.UseMiddleware<AuditMiddleware>();
    }
}
