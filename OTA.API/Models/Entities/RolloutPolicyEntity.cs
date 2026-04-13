using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using OTA.API.Models.Enums;

namespace OTA.API.Models.Entities
{
    /// <summary>
    /// MongoDB document representing a reusable rollout policy that governs execution parameters
    /// for OTA rollout campaigns. Policies can be shared across multiple rollouts within a project.
    /// Collection: rollout_policies
    /// Indexes:
    ///   - Unique: PolicyId
    ///   - Single: IsActive
    ///   - Text: Name (for search)
    /// </summary>
    public sealed class RolloutPolicyEntity
    {
        /// <summary>MongoDB internal ObjectId (_id).</summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Platform-generated unique identifier for this policy (GUID string).</summary>
        [BsonElement("policyId")]
        public string PolicyId { get; set; } = string.Empty;

        /// <summary>Human-readable policy name (e.g., "Conservative Production Policy").</summary>
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Optional description explaining the intended use-case for this policy.</summary>
        [BsonElement("description")]
        [BsonIgnoreIfNull]
        public string? Description { get; set; }

        /// <summary>
        /// Maximum number of OTA jobs that may be in the InProgress state simultaneously
        /// across the entire rollout. Prevents overloading the device fleet or download infrastructure.
        /// </summary>
        [BsonElement("maxConcurrentDevices")]
        public int MaxConcurrentDevices { get; set; } = 50;

        /// <summary>
        /// Maximum number of times a failed OTA job will be automatically retried
        /// before it is marked as permanently Failed.
        /// </summary>
        [BsonElement("retryLimit")]
        public int RetryLimit { get; set; } = 3;

        /// <summary>
        /// Interval in minutes between automatic retry attempts for failed OTA jobs.
        /// Retries use a fixed interval (not exponential back-off) at the job level.
        /// </summary>
        [BsonElement("retryIntervalMinutes")]
        public int RetryIntervalMinutes { get; set; } = 30;

        /// <summary>
        /// Whether this policy permits firmware downgrade operations (installing an older version
        /// than the device's current firmware). Disabled by default for safety.
        /// </summary>
        [BsonElement("allowDowngrade")]
        public bool AllowDowngrade { get; set; } = false;

        /// <summary>
        /// If true, the rollout will be blocked from transitioning to Active unless the selected
        /// firmware version has Status == QAVerified or Approved.
        /// </summary>
        [BsonElement("requireQAVerification")]
        public bool RequireQAVerification { get; set; } = true;

        /// <summary>
        /// If true, the firmware approval step requires sign-off from two distinct ReleaseManager
        /// or higher users before the firmware may be used in a production rollout.
        /// </summary>
        [BsonElement("requireDualApproval")]
        public bool RequireDualApproval { get; set; } = false;

        /// <summary>Execution phase strategy applied when using this policy.</summary>
        [BsonElement("rolloutPhase")]
        [BsonRepresentation(BsonType.String)]
        public RolloutPhase RolloutPhase { get; set; } = RolloutPhase.Full;

        /// <summary>
        /// For Canary phase: the percentage of the total target device population to
        /// receive the firmware in the first wave. Valid range: 1–50.
        /// </summary>
        [BsonElement("canaryPercentage")]
        public int CanaryPercentage { get; set; } = 5;

        /// <summary>
        /// For Rolling phase: the number of devices to include in each successive batch.
        /// The next batch is only dispatched after the current batch reaches a terminal state.
        /// </summary>
        [BsonElement("rollingBatchSize")]
        public int RollingBatchSize { get; set; } = 100;

        /// <summary>UTC timestamp when this policy was created.</summary>
        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>UTC timestamp of the most recent update to this policy.</summary>
        [BsonElement("updatedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>UserId of the platform user who created this policy.</summary>
        [BsonElement("createdByUserId")]
        public string CreatedByUserId { get; set; } = string.Empty;

        /// <summary>
        /// Whether this policy is active and available for assignment to new rollouts.
        /// Inactive policies cannot be assigned but existing rollouts referencing them are unaffected.
        /// </summary>
        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        // ── Alias properties for service layer compatibility ───────────────────
        [BsonIgnore] public int BatchSize { get => RollingBatchSize; set => RollingBatchSize = value; }
        [BsonIgnore] public int ConcurrencyLimit { get => MaxConcurrentDevices; set => MaxConcurrentDevices = value; }
        [BsonIgnore] public int RetryDelaySeconds { get => RetryIntervalMinutes * 60; set => RetryIntervalMinutes = value / 60; }
    }
}
