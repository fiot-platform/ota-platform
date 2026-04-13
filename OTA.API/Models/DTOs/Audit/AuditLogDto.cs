using OTA.API.Models.Enums;

namespace OTA.API.Models.DTOs.Audit
{
    public class AuditLogDto
    {
        public string AuditId { get; set; } = string.Empty;
        public AuditAction Action { get; set; }
        public string ActionName => Action.ToString();
        public string? PerformedByUserId { get; set; }
        public string? PerformedByEmail { get; set; }
        public string? PerformedByRole { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, string> AdditionalContext { get; set; } = new();
    }

    public class AuditSearchFilter
    {
        public string? UserId { get; set; }
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public AuditAction? Action { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
