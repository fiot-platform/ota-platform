using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using OTA.API.Models.Enums;

namespace OTA.API.Models.Entities
{
    /// <summary>
    /// MongoDB document storing an immutable audit log entry for every significant mutating
    /// action performed within the OTA platform. Audit records are never updated or deleted
    /// in normal operation; they are append-only to preserve the full change history.
    /// Collection: audit_logs
    /// Indexes:
    ///   - Unique: AuditId
    ///   - Single: PerformedByUserId
    ///   - Single: Action
    ///   - Single: EntityType
    ///   - Compound: {EntityType, EntityId}
    ///   - Single: Timestamp (descending)
    ///   - Compound: {PerformedByUserId, Timestamp} (for user activity queries)
    ///   - TTL: Timestamp (retain for 7 years for compliance)
    /// </summary>
    public sealed class AuditLogEntity
    {
        /// <summary>MongoDB internal ObjectId (_id).</summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Platform-generated unique identifier for this audit entry (GUID string).</summary>
        [BsonElement("auditId")]
        public string AuditId { get; set; } = string.Empty;

        /// <summary>The specific auditable action that was performed.</summary>
        [BsonElement("action")]
        [BsonRepresentation(BsonType.String)]
        public AuditAction Action { get; set; }

        /// <summary>UserId of the user (or system process) that performed the action.</summary>
        [BsonElement("performedByUserId")]
        [BsonIgnoreIfNull]
        public string? PerformedByUserId { get; set; }

        /// <summary>Email address of the user who performed the action (denormalised for audit readability).</summary>
        [BsonElement("performedByEmail")]
        [BsonIgnoreIfNull]
        public string? PerformedByEmail { get; set; }

        /// <summary>Role of the user who performed the action at the time of the action.</summary>
        [BsonElement("performedByRole")]
        [BsonIgnoreIfNull]
        public string? PerformedByRole { get; set; }

        /// <summary>
        /// The type of entity that was affected by the action
        /// (e.g., "User", "FirmwareVersion", "Rollout", "Device").
        /// </summary>
        [BsonElement("entityType")]
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// The platform identifier of the specific entity instance that was affected
        /// (e.g., a UserId, FirmwareId, RolloutId).
        /// </summary>
        [BsonElement("entityId")]
        [BsonIgnoreIfNull]
        public string? EntityId { get; set; }

        /// <summary>
        /// JSON-serialised snapshot of the entity's state before the action was applied.
        /// Null for create actions where no prior state existed.
        /// </summary>
        [BsonElement("oldValue")]
        [BsonIgnoreIfNull]
        public string? OldValue { get; set; }

        /// <summary>
        /// JSON-serialised snapshot of the entity's state after the action was applied.
        /// Null for delete/deactivate actions.
        /// </summary>
        [BsonElement("newValue")]
        [BsonIgnoreIfNull]
        public string? NewValue { get; set; }

        /// <summary>Client IP address from which the request originated (IPv4 or IPv6).</summary>
        [BsonElement("ipAddress")]
        [BsonIgnoreIfNull]
        public string? IpAddress { get; set; }

        /// <summary>User-Agent header value of the HTTP client that performed the request.</summary>
        [BsonElement("userAgent")]
        [BsonIgnoreIfNull]
        public string? UserAgent { get; set; }

        /// <summary>UTC timestamp when this audit event occurred.</summary>
        [BsonElement("timestamp")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Additional contextual data relevant to the action that does not fit the standard schema
        /// (e.g., rollout batch number, retry attempt index, webhook delivery ID).
        /// </summary>
        [BsonElement("additionalContext")]
        public Dictionary<string, string> AdditionalContext { get; set; } = new();

        /// <summary>Alias for <see cref="AdditionalContext"/> used by AuditService.</summary>
        [BsonIgnore]
        public Dictionary<string, string> Context
        {
            get => AdditionalContext;
            set => AdditionalContext = value;
        }

        /// <summary>Alias for <see cref="Timestamp"/> — when this entry was created.</summary>
        [BsonIgnore]
        public DateTime CreatedAt
        {
            get => Timestamp;
            set => Timestamp = value;
        }

        /// <summary>UpdatedAt — audit entries are immutable; always equals CreatedAt.</summary>
        [BsonIgnore]
        public DateTime UpdatedAt
        {
            get => Timestamp;
            set { /* immutable */ }
        }
    }
}
