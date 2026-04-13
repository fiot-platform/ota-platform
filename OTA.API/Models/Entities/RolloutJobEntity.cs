using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using OTA.API.Models.Enums;

namespace OTA.API.Models.Entities
{
    /// <summary>
    /// MongoDB document representing an individual device-level update job within a rollout.
    /// Collection: rolloutJobs
    /// Indexes:
    ///   - Unique: JobId
    ///   - Compound: {RolloutId, DeviceId}
    ///   - Single: Status
    ///   - Single: DeviceId
    /// </summary>
    public sealed class RolloutJobEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("jobId")]
        public string JobId { get; set; } = string.Empty;

        [BsonElement("rolloutId")]
        public string RolloutId { get; set; } = string.Empty;

        [BsonElement("deviceId")]
        public string DeviceId { get; set; } = string.Empty;

        [BsonElement("firmwareId")]
        public string FirmwareId { get; set; } = string.Empty;

        [BsonElement("firmwareVersion")]
        public string FirmwareVersion { get; set; } = string.Empty;

        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public JobStatus Status { get; set; } = JobStatus.Pending;

        [BsonElement("retryCount")]
        public int RetryCount { get; set; } = 0;

        [BsonElement("maxRetries")]
        public int MaxRetries { get; set; } = 3;

        [BsonElement("errorMessage")]
        [BsonIgnoreIfNull]
        public string? ErrorMessage { get; set; }

        [BsonElement("startedAt")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? StartedAt { get; set; }

        [BsonElement("completedAt")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? CompletedAt { get; set; }

        [BsonElement("lastRetriedAt")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? LastRetriedAt { get; set; }

        [BsonElement("deviceAcknowledgedAt")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? DeviceAcknowledgedAt { get; set; }

        [BsonElement("downloadProgressPercent")]
        public int DownloadProgressPercent { get; set; }

        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
