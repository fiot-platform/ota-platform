namespace OTA.API.Helpers
{
    /// <summary>
    /// Scoped service that carries audit context for the duration of a single HTTP request.
    /// Populated by <see cref="OTA.API.Middleware.AuditMiddleware"/> from the authenticated
    /// user's JWT claims and request metadata.
    /// Services that write audit log entries inject this class to obtain the caller context
    /// without re-parsing the HTTP context themselves.
    /// </summary>
    public class AuditContext
    {
        /// <summary>
        /// Platform user identifier extracted from the JWT 'sub' or 'userId' claim.
        /// Null for unauthenticated requests (e.g., webhook endpoints).
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Email address extracted from the JWT 'email' claim.
        /// Null for unauthenticated requests.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Role name extracted from the JWT 'role' claim.
        /// Null for unauthenticated requests.
        /// </summary>
        public string? Role { get; set; }

        /// <summary>
        /// IPv4 or IPv6 address of the HTTP client. Respects X-Forwarded-For when behind a proxy.
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// Raw User-Agent header value from the incoming HTTP request.
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// Returns true if the audit context was populated from an authenticated request.
        /// </summary>
        public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);
    }
}
