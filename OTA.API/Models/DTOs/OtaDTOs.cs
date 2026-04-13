using System.ComponentModel.DataAnnotations;
using OTA.API.Models.Enums;

namespace OTA.API.Models.DTOs
{
    // ─────────────────────────────────────────────────────────────────────────
    // OTA Rollout & Job DTOs
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Request body for creating a new OTA rollout campaign.</summary>
    public sealed class CreateRolloutRequest
    {
        /// <summary>Human-readable rollout name (e.g., "Q2-2026 Gateway Firmware Rollout").</summary>
        [Required(ErrorMessage = "Rollout name is required.")]
        [MinLength(3)]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Optional description of the rollout's purpose.</summary>
        [MaxLength(2000)]
        public string? Description { get; set; }

        /// <summary>ProjectId under which this rollout is executed.</summary>
        [Required(ErrorMessage = "ProjectId is required.")]
        [MaxLength(36)]
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>FirmwareId of the approved firmware version to deploy.</summary>
        [Required(ErrorMessage = "FirmwareId is required.")]
        [MaxLength(36)]
        public string FirmwareId { get; set; } = string.Empty;

        /// <summary>How the target device set is resolved.</summary>
        [Required]
        public TargetType TargetType { get; set; } = TargetType.AllDevices;

        /// <summary>
        /// Target identifiers; interpretation depends on TargetType.
        /// For AllDevices this may be empty.
        /// </summary>
        public List<string> TargetIds { get; set; } = new();

        /// <summary>Optional channel filter applied when resolving target devices.</summary>
        [MaxLength(50)]
        public string? Channel { get; set; }

        /// <summary>Optional PolicyId of a RolloutPolicy to govern execution parameters.</summary>
        [MaxLength(36)]
        public string? PolicyId { get; set; }

        /// <summary>Execution phase strategy for this rollout.</summary>
        public RolloutPhase Phase { get; set; } = RolloutPhase.Full;

        /// <summary>Optional future UTC start time. If null, the rollout starts immediately on activation.</summary>
        public DateTime? ScheduledAt { get; set; }

        /// <summary>Optional operational notes (e.g., change ticket reference).</summary>
        [MaxLength(1000)]
        public string? Notes { get; set; }
    }

    /// <summary>Request body for updating mutable rollout properties before activation.</summary>
    public sealed class UpdateRolloutRequest
    {
        /// <summary>Updated rollout name.</summary>
        [MinLength(3)]
        [MaxLength(200)]
        public string? Name { get; set; }

        /// <summary>Updated description.</summary>
        [MaxLength(2000)]
        public string? Description { get; set; }

        /// <summary>Updated target type.</summary>
        public TargetType? TargetType { get; set; }

        /// <summary>Updated target IDs list.</summary>
        public List<string>? TargetIds { get; set; }

        /// <summary>Updated channel filter.</summary>
        [MaxLength(50)]
        public string? Channel { get; set; }

        /// <summary>Updated policy reference.</summary>
        [MaxLength(36)]
        public string? PolicyId { get; set; }

        /// <summary>Updated execution phase.</summary>
        public RolloutPhase? Phase { get; set; }

        /// <summary>Updated scheduled start time.</summary>
        public DateTime? ScheduledAt { get; set; }

        /// <summary>Updated operational notes.</summary>
        [MaxLength(1000)]
        public string? Notes { get; set; }
    }

    /// <summary>Summary row for paginated rollout list responses.</summary>
    public sealed class RolloutListDto
    {
        public string RolloutId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string FirmwareVersion { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Phase { get; set; } = string.Empty;
        public int TotalDevices { get; set; }
        public int SucceededCount { get; set; }
        public int FailedCount { get; set; }
        public int CancelledCount { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>Computed completion percentage (0–100).</summary>
        public double CompletionPercentage =>
            TotalDevices == 0 ? 0 : Math.Round((double)(SucceededCount + FailedCount + CancelledCount) / TotalDevices * 100, 1);
    }

    /// <summary>Full rollout detail returned by GET /api/rollouts/{rolloutId}.</summary>
    public class RolloutDetailDto
    {
        // Alias properties for service layer compatibility
        public string Id { get; set; } = string.Empty;

        public string RolloutId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ProjectId { get; set; } = string.Empty;
        public string FirmwareId { get; set; } = string.Empty;
        public string FirmwareVersion { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public List<string> TargetIds { get; set; } = new();
        public string? Channel { get; set; }
        public string? PolicyId { get; set; }
        public string Phase { get; set; } = string.Empty;
        public int TotalDevices { get; set; }
        public int SucceededCount { get; set; }
        public int FailedCount { get; set; }
        public int CancelledCount { get; set; }
        public int PendingCount => TotalDevices - SucceededCount - FailedCount - CancelledCount;
        public int TotalDeviceCount { get => TotalDevices; set => TotalDevices = value; }
        public int SuccessCount { get => SucceededCount; set => SucceededCount = value; }
        public int FailureCount { get => FailedCount; set => FailedCount = value; }
        public DateTime? ScheduledAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? PausedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    /// <summary>Individual OTA job record returned in job list and detail responses.</summary>
    public sealed class OtaJobDto
    {
        public string Id { get; set; } = string.Empty;
        public string JobId { get; set; } = string.Empty;
        public string RolloutId { get; set; } = string.Empty;
        public string FirmwareId { get; set; } = string.Empty;
        public string FirmwareVersion { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceSerialNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int AttemptCount { get; set; }
        public DateTime? LastAttemptAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? FailureReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Alias properties for service layer compatibility
        public string? TargetVersion { get; set; }
        public int RetryCount { get => AttemptCount; set => AttemptCount = value; }
        public string? ErrorMessage { get => FailureReason; set => FailureReason = value; }
    }

    /// <summary>Request body for manually retrying a failed OTA job.</summary>
    public sealed class RetryJobRequest
    {
        /// <summary>Optional override reason for the retry (recorded in audit log).</summary>
        [MaxLength(500)]
        public string? Reason { get; set; }
    }

    /// <summary>Aggregated job status counts for a rollout, used in dashboard and detail views.</summary>
    public sealed class RolloutSummaryDto
    {
        public string RolloutId { get; set; } = string.Empty;
        public string RolloutName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int TotalDevices { get; set; }
        public int CreatedCount { get; set; }
        public int QueuedCount { get; set; }
        public int InProgressCount { get; set; }
        public int SucceededCount { get; set; }
        public int FailedCount { get; set; }
        public int CancelledCount { get; set; }
        public int PausedCount { get; set; }

        /// <summary>Computed success rate as a percentage (0–100).</summary>
        public double SuccessRate =>
            TotalDevices == 0 ? 0 : Math.Round((double)SucceededCount / TotalDevices * 100, 1);
    }

    /// <summary>Wrapper for paginated rollout list responses.</summary>
    public sealed class PagedRolloutListResponse
    {
        public List<RolloutListDto> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public long TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    /// <summary>Wrapper for paginated OTA job list responses.</summary>
    public sealed class PagedOtaJobListResponse
    {
        public List<OtaJobDto> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public long TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
