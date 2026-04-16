using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using OTA.API.Models.Enums;

namespace OTA.API.Models.Entities
{
    // ── Embedded sub-documents ────────────────────────────────────────────────

    /// <summary>A test case or test result document file attached to a QA session.</summary>
    public sealed class QADocumentItem
    {
        [BsonElement("documentId")]
        public string DocumentId { get; set; } = Guid.NewGuid().ToString("N");

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("storedFileName")]
        public string StoredFileName { get; set; } = string.Empty;

        [BsonElement("downloadUrl")]
        public string DownloadUrl { get; set; } = string.Empty;

        [BsonElement("fileSizeBytes")]
        public long FileSizeBytes { get; set; }

        [BsonElement("uploadedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("uploadedByUserId")]
        public string UploadedByUserId { get; set; } = string.Empty;
    }

    /// <summary>A bug raised during QA testing of a firmware version.</summary>
    public sealed class QABugItem
    {
        [BsonElement("bugId")]
        public string BugId { get; set; } = Guid.NewGuid().ToString("N");

        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("description")]
        [BsonIgnoreIfNull]
        public string? Description { get; set; }

        [BsonElement("severity")]
        [BsonRepresentation(BsonType.String)]
        public BugSeverity Severity { get; set; } = BugSeverity.Medium;

        [BsonElement("bugStatus")]
        [BsonRepresentation(BsonType.String)]
        public BugStatus BugStatus { get; set; } = BugStatus.Open;

        [BsonElement("reportedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime ReportedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("reportedByUserId")]
        public string ReportedByUserId { get; set; } = string.Empty;

        [BsonElement("resolvedAt")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? ResolvedAt { get; set; }

        [BsonElement("resolution")]
        [BsonIgnoreIfNull]
        public string? Resolution { get; set; }
    }

    /// <summary>An event log entry capturing a state change or action within a QA session.</summary>
    public sealed class QAEventLogItem
    {
        [BsonElement("eventId")]
        public string EventId { get; set; } = Guid.NewGuid().ToString("N");

        [BsonElement("eventType")]
        public string EventType { get; set; } = string.Empty;

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("userId")]
        public string UserId { get; set; } = string.Empty;

        [BsonElement("timestamp")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [BsonElement("metadata")]
        [BsonIgnoreIfNull]
        public Dictionary<string, string>? Metadata { get; set; }
    }

    // ── Root document ─────────────────────────────────────────────────────────

    /// <summary>
    /// MongoDB document representing the QA testing session for a single firmware version.
    /// Collection: QASessions
    /// One session per firmware version (unique index on firmwareId).
    /// </summary>
    public sealed class QASessionEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonElement("firmwareId")]
        public string FirmwareId { get; set; } = string.Empty;

        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public QASessionStatus Status { get; set; } = QASessionStatus.NotStarted;

        [BsonElement("testCaseDocuments")]
        public List<QADocumentItem> TestCaseDocuments { get; set; } = new();

        [BsonElement("testResultDocuments")]
        public List<QADocumentItem> TestResultDocuments { get; set; } = new();

        [BsonElement("bugs")]
        public List<QABugItem> Bugs { get; set; } = new();

        [BsonElement("eventLog")]
        public List<QAEventLogItem> EventLog { get; set; } = new();

        [BsonElement("startedAt")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? StartedAt { get; set; }

        [BsonElement("startedByUserId")]
        [BsonIgnoreIfNull]
        public string? StartedByUserId { get; set; }

        [BsonElement("startedByName")]
        [BsonIgnoreIfNull]
        public string? StartedByName { get; set; }

        [BsonElement("completedAt")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? CompletedAt { get; set; }

        [BsonElement("remarks")]
        [BsonIgnoreIfNull]
        public string? Remarks { get; set; }

        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("createdByUserId")]
        public string CreatedByUserId { get; set; } = string.Empty;
    }
}
