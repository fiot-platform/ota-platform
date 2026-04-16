using System.ComponentModel.DataAnnotations;
using OTA.API.Models.Enums;

namespace OTA.API.Models.DTOs
{
    // ─────────────────────────────────────────────────────────────────────────
    // QA Session Request DTOs
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Request to update the overall status of a QA session.</summary>
    public sealed class UpdateQAStatusRequest
    {
        [Required]
        public QASessionStatus Status { get; set; }

        [MaxLength(2000)]
        public string? Remarks { get; set; }
    }

    /// <summary>Request to add a new bug to the QA session bug list.</summary>
    public sealed class AddBugRequest
    {
        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(5000)]
        public string? Description { get; set; }

        public BugSeverity Severity { get; set; } = BugSeverity.Medium;
    }

    /// <summary>Request to update an existing bug in the QA session.</summary>
    public sealed class UpdateBugRequest
    {
        [MaxLength(500)]
        public string? Title { get; set; }

        [MaxLength(5000)]
        public string? Description { get; set; }

        public BugSeverity? Severity { get; set; }

        public BugStatus? BugStatus { get; set; }

        [MaxLength(2000)]
        public string? Resolution { get; set; }
    }

    /// <summary>Request to finalize a QA session as either Complete or Fail.</summary>
    public sealed class CompleteQARequest
    {
        [Required]
        public QASessionStatus FinalStatus { get; set; }

        [MaxLength(2000)]
        public string? Remarks { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // QA Session Response DTOs
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Full read-only representation of a QA session.</summary>
    public sealed class QASessionDto
    {
        public string SessionId { get; set; } = string.Empty;
        public string FirmwareId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<QADocumentItemDto> TestCaseDocuments { get; set; } = new();
        public List<QADocumentItemDto> TestResultDocuments { get; set; } = new();
        public List<QABugItemDto> Bugs { get; set; } = new();
        public List<QAEventLogItemDto> EventLog { get; set; } = new();
        public DateTime? StartedAt { get; set; }
        public string? StartedByUserId { get; set; }
        public string? StartedByName { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Computed summary fields
        public int TotalBugs => Bugs.Count;
        public int OpenBugs => Bugs.Count(b => b.BugStatus is "Open" or "InProgress");
        public int ResolvedBugs => Bugs.Count(b => b.BugStatus is "Resolved" or "WontFix");
        public int TotalTestCaseDocs => TestCaseDocuments.Count;
        public int TotalTestResultDocs => TestResultDocuments.Count;
    }

    /// <summary>Representation of a document attached to a QA session.</summary>
    public sealed class QADocumentItemDto
    {
        public string DocumentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime UploadedAt { get; set; }
        public string UploadedByUserId { get; set; } = string.Empty;
    }

    /// <summary>Representation of a single bug in a QA session.</summary>
    public sealed class QABugItemDto
    {
        public string BugId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string BugStatus { get; set; } = string.Empty;
        public DateTime ReportedAt { get; set; }
        public string ReportedByUserId { get; set; } = string.Empty;
        public DateTime? ResolvedAt { get; set; }
        public string? Resolution { get; set; }
    }

    /// <summary>Representation of an event log entry in a QA session.</summary>
    public sealed class QAEventLogItemDto
    {
        public string EventId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }

    /// <summary>Response returned after a QA document is uploaded successfully.</summary>
    public sealed class UploadQADocumentResponse
    {
        public string DocumentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
