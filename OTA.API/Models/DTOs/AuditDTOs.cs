using System.ComponentModel.DataAnnotations;
using OTA.API.Models.Enums;

namespace OTA.API.Models.DTOs
{
    // ─────────────────────────────────────────────────────────────────────────
    // Audit Log DTOs
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Full audit log entry as returned by the audit log list and detail endpoints.</summary>
    public sealed class AuditLogDto
    {
        public string AuditId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? PerformedByUserId { get; set; }
        public string? PerformedByEmail { get; set; }
        public string? PerformedByRole { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string? EntityId { get; set; }

        /// <summary>JSON snapshot of entity state before the action. Null for create operations.</summary>
        public string? OldValue { get; set; }

        /// <summary>JSON snapshot of entity state after the action. Null for delete/deactivate operations.</summary>
        public string? NewValue { get; set; }

        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, string> AdditionalContext { get; set; } = new();
    }

    /// <summary>
    /// Filter parameters for the paginated audit log query.
    /// All filters are optional and applied as AND conditions.
    /// </summary>
    public sealed class AuditLogFilterRequest
    {
        /// <summary>Inclusive start of the timestamp range (UTC).</summary>
        public DateTime? DateFrom { get; set; }

        /// <summary>Inclusive end of the timestamp range (UTC).</summary>
        public DateTime? DateTo { get; set; }

        /// <summary>Filter by a specific audit action (e.g., FirmwareApproved).</summary>
        public AuditAction? Action { get; set; }

        /// <summary>Filter by the UserId of the user who performed the action.</summary>
        [MaxLength(36)]
        public string? UserId { get; set; }

        /// <summary>Filter by entity type (e.g., "FirmwareVersion", "Rollout").</summary>
        [MaxLength(100)]
        public string? EntityType { get; set; }

        /// <summary>Filter by the specific entity instance identifier.</summary>
        [MaxLength(36)]
        public string? EntityId { get; set; }

        /// <summary>Filter by the role of the user who performed the action.</summary>
        [MaxLength(50)]
        public string? Role { get; set; }

        /// <summary>Free-text search against email or entity type fields.</summary>
        [MaxLength(200)]
        public string? SearchText { get; set; }

        /// <summary>Page number (1-based). Defaults to 1.</summary>
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        /// <summary>Number of records per page. Defaults to 25; maximum 200.</summary>
        [Range(1, 200)]
        public int PageSize { get; set; } = 25;
    }

    /// <summary>Request body for exporting audit log data to a file (CSV or JSON).</summary>
    public sealed class AuditExportRequest
    {
        /// <summary>Inclusive start of the export date range (UTC). Required.</summary>
        [Required(ErrorMessage = "DateFrom is required for export.")]
        public DateTime DateFrom { get; set; }

        /// <summary>Inclusive end of the export date range (UTC). Required.</summary>
        [Required(ErrorMessage = "DateTo is required for export.")]
        public DateTime DateTo { get; set; }

        /// <summary>Optional action filter.</summary>
        public AuditAction? Action { get; set; }

        /// <summary>Optional entity type filter.</summary>
        [MaxLength(100)]
        public string? EntityType { get; set; }

        /// <summary>Optional user filter.</summary>
        [MaxLength(36)]
        public string? UserId { get; set; }

        /// <summary>Export format: "csv" or "json". Defaults to "csv".</summary>
        [RegularExpression("^(csv|json)$", ErrorMessage = "Format must be 'csv' or 'json'.")]
        public string Format { get; set; } = "csv";
    }

    /// <summary>Wrapper for paginated audit log list responses.</summary>
    public sealed class PagedAuditLogResponse
    {
        public List<AuditLogDto> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public long TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
