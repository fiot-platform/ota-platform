using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using OTA.API.Models.Enums;

namespace OTA.API.Models.Entities
{
    /// <summary>
    /// MongoDB document representing a single OTA firmware update job assigned to one device
    /// as part of a parent rollout campaign.
    /// Collection: ota_jobs
    /// Indexes:
    ///   - Unique: JobId
    ///   - Single: RolloutId
    ///   - Single: DeviceId
    ///   - Compound: {DeviceId, Status}
    ///   - Compound: {RolloutId, Status}
    ///   - Single: Status (for background monitor queries)
    /// </summary>
    public sealed class OtaJobEntity
    {
        /// <summary>MongoDB internal ObjectId (_id).</summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Platform-generated unique identifier for this job (GUID string).
        /// Returned to the device in the check-update response so it can report progress.
        /// </summary>
        [BsonElement("jobId")]
        public string JobId { get; set; } = string.Empty;

        /// <summary>RolloutId of the parent rollout campaign that spawned this job.</summary>
        [BsonElement("rolloutId")]
        public string RolloutId { get; set; } = string.Empty;

        /// <summary>FirmwareId of the firmware version to be installed by this job.</summary>
        [BsonElement("firmwareId")]
        public string FirmwareId { get; set; } = string.Empty;

        /// <summary>Denormalised firmware version string for quick display without a join.</summary>
        [BsonElement("firmwareVersion")]
        public string FirmwareVersion { get; set; } = string.Empty;

        /// <summary>DeviceId of the device that this job targets.</summary>
        [BsonElement("deviceId")]
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>Denormalised device serial number for quick display without a join.</summary>
        [BsonElement("deviceSerialNumber")]
        public string DeviceSerialNumber { get; set; } = string.Empty;

        /// <summary>Current processing status of this job.</summary>
        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public OtaJobStatus Status { get; set; } = OtaJobStatus.Created;

        /// <summary>
        /// Total number of delivery attempts made for this job.
        /// Incremented each time the job transitions to InProgress after a failure.
        /// </summary>
        [BsonElement("attemptCount")]
        public int AttemptCount { get; set; } = 0;

        /// <summary>UTC timestamp of the most recent delivery attempt.</summary>
        [BsonElement("lastAttemptAt")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? LastAttemptAt { get; set; }

        /// <summary>UTC timestamp when the device first acknowledged the job (transitioned to InProgress).</summary>
        [BsonElement("startedAt")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? StartedAt { get; set; }

        /// <summary>UTC timestamp when the job reached a terminal status (Succeeded, Failed, or Cancelled).</summary>
        [BsonElement("completedAt")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Human-readable failure reason reported by the device or set by the platform
        /// when the job fails. Null when the job has not failed.
        /// </summary>
        [BsonElement("failureReason")]
        [BsonIgnoreIfNull]
        public string? FailureReason { get; set; }

        // ── Acknowledgement (SuperAdmin / ReleaseManager must approve before device picks up) ──

        /// <summary>UserId of the platform user who acknowledged or rejected this job.</summary>
        [BsonElement("acknowledgedByUserId")]
        [BsonIgnoreIfNull]
        public string? AcknowledgedByUserId { get; set; }

        /// <summary>Display name of the user who acknowledged or rejected this job.</summary>
        [BsonElement("acknowledgedByName")]
        [BsonIgnoreIfNull]
        public string? AcknowledgedByName { get; set; }

        /// <summary>UTC timestamp when a SuperAdmin or ReleaseManager acknowledged this job.</summary>
        [BsonElement("acknowledgedAt")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? AcknowledgedAt { get; set; }

        /// <summary>Optional notes left by the approver / rejector.</summary>
        [BsonElement("acknowledgementNotes")]
        [BsonIgnoreIfNull]
        public string? AcknowledgementNotes { get; set; }

        /// <summary>UTC timestamp when this job record was created.</summary>
        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>UTC timestamp of the most recent update to this job record.</summary>
        [BsonElement("updatedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ── Alias properties for service layer compatibility ───────────────────
        [BsonIgnore] public string? TargetVersion { get; set; }
        [BsonIgnore] public string? ErrorMessage { get => FailureReason; set => FailureReason = value; }
        [BsonIgnore] public int? RetryCount { get => AttemptCount; set => AttemptCount = value ?? 0; }
    }
}
