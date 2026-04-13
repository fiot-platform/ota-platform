using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using OTA.API.Models.Enums;

namespace OTA.API.Models.Entities
{
    /// <summary>
    /// MongoDB document representing an OTA rollout campaign that targets one or more devices
    /// with a specific approved firmware version.
    /// Collection: rollouts
    /// Indexes:
    ///   - Unique: RolloutId
    ///   - Single: ProjectId
    ///   - Single: FirmwareId
    ///   - Single: Status
    ///   - Compound: {ProjectId, Status}
    ///   - Single: ScheduledAt (for scheduler queries)
    /// </summary>
    public sealed class RolloutEntity
    {
        /// <summary>MongoDB internal ObjectId (_id).</summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Platform-generated unique identifier for this rollout (GUID string).</summary>
        [BsonElement("rolloutId")]
        public string RolloutId { get; set; } = string.Empty;

        /// <summary>Human-readable name for the rollout campaign (e.g., "Q2-2026 Gateway Firmware Rollout").</summary>
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Optional free-text description of the rollout's purpose and scope.</summary>
        [BsonElement("description")]
        [BsonIgnoreIfNull]
        public string? Description { get; set; }

        /// <summary>ProjectId of the project under which this rollout is executed.</summary>
        [BsonElement("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>FirmwareId of the approved firmware version to be deployed.</summary>
        [BsonElement("firmwareId")]
        public string FirmwareId { get; set; } = string.Empty;

        /// <summary>Denormalised firmware version string for display without a join.</summary>
        [BsonElement("firmwareVersion")]
        public string FirmwareVersion { get; set; } = string.Empty;

        /// <summary>Current lifecycle status of this rollout campaign.</summary>
        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public RolloutStatus Status { get; set; } = RolloutStatus.Draft;

        /// <summary>Defines how the set of target devices is resolved for this rollout.</summary>
        [BsonElement("targetType")]
        [BsonRepresentation(BsonType.String)]
        public TargetType TargetType { get; set; } = TargetType.AllDevices;

        /// <summary>
        /// List of target identifiers whose interpretation depends on TargetType:
        /// - AllDevices: empty (all devices in project scope)
        /// - DeviceGroup: group IDs
        /// - Site: site IDs
        /// - Channel: channel names (e.g., ["Beta"])
        /// - SpecificDevices: individual DeviceId values
        /// </summary>
        [BsonElement("targetIds")]
        public List<string> TargetIds { get; set; } = new();

        /// <summary>Firmware channel filter applied when resolving devices (e.g., "Beta").</summary>
        [BsonElement("channel")]
        [BsonIgnoreIfNull]
        public string? Channel { get; set; }

        /// <summary>PolicyId of the RolloutPolicy governing retry, concurrency, and phase settings.</summary>
        [BsonElement("policyId")]
        [BsonIgnoreIfNull]
        public string? PolicyId { get; set; }

        /// <summary>Execution phase strategy for this rollout.</summary>
        [BsonElement("phase")]
        [BsonRepresentation(BsonType.String)]
        public RolloutPhase Phase { get; set; } = RolloutPhase.Full;

        /// <summary>Total number of devices targeted by this rollout (computed at rollout creation).</summary>
        [BsonElement("totalDevices")]
        public int TotalDevices { get; set; }

        /// <summary>Count of devices that have successfully installed the firmware.</summary>
        [BsonElement("succeededCount")]
        public int SucceededCount { get; set; }

        /// <summary>Count of devices whose OTA job has failed after all retries.</summary>
        [BsonElement("failedCount")]
        public int FailedCount { get; set; }

        /// <summary>Count of devices whose OTA job was cancelled.</summary>
        [BsonElement("cancelledCount")]
        public int CancelledCount { get; set; }

        /// <summary>UTC timestamp when the rollout is scheduled to begin (null for immediate start).</summary>
        [BsonElement("scheduledAt")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? ScheduledAt { get; set; }

        /// <summary>UTC timestamp when the rollout actually started executing.</summary>
        [BsonElement("startedAt")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? StartedAt { get; set; }

        /// <summary>UTC timestamp when the rollout reached the Completed status.</summary>
        [BsonElement("completedAt")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? CompletedAt { get; set; }

        /// <summary>UTC timestamp when the rollout was most recently paused by an operator.</summary>
        [BsonElement("pausedAt")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? PausedAt { get; set; }

        /// <summary>UTC timestamp when this rollout record was created.</summary>
        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>UTC timestamp of the most recent update to this rollout record.</summary>
        [BsonElement("updatedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>UserId of the platform user who created this rollout.</summary>
        [BsonElement("createdByUserId")]
        public string CreatedByUserId { get; set; } = string.Empty;

        /// <summary>Optional operational notes added by the operator (e.g., change ticket reference).</summary>
        [BsonElement("notes")]
        [BsonIgnoreIfNull]
        public string? Notes { get; set; }

        // ── Alias properties for service layer compatibility ───────────────────

        [BsonIgnore] public int TotalDeviceCount { get => TotalDevices; set => TotalDevices = value; }
        [BsonIgnore] public int PendingCount { get; set; }
        [BsonIgnore] public int SuccessCount { get => SucceededCount; set => SucceededCount = value; }
        [BsonIgnore] public int FailureCount { get => FailedCount; set => FailedCount = value; }
        [BsonIgnore] public int BatchSize { get; set; }
        [BsonIgnore] public int ConcurrencyLimit { get; set; }
    }
}
