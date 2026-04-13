using System.ComponentModel.DataAnnotations;
using OTA.API.Models.Enums;

namespace OTA.API.Models.DTOs
{
    // ─────────────────────────────────────────────────────────────────────────
    // Rollout Policy DTOs
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Request body for creating a new rollout policy.</summary>
    public class CreatePolicyRequest
    {
        /// <summary>Human-readable policy name; must be unique within the platform.</summary>
        [Required(ErrorMessage = "Policy name is required.")]
        [MinLength(3)]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Optional description explaining the intended use-case for this policy.</summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>Maximum number of OTA jobs in InProgress state simultaneously. Minimum 1.</summary>
        [Range(1, 10000, ErrorMessage = "MaxConcurrentDevices must be between 1 and 10000.")]
        public int MaxConcurrentDevices { get; set; } = 50;

        /// <summary>Maximum retry attempts for each failed OTA job. Range: 0–10.</summary>
        [Range(0, 10, ErrorMessage = "RetryLimit must be between 0 and 10.")]
        public int RetryLimit { get; set; } = 3;

        /// <summary>Fixed interval in minutes between retry attempts. Range: 5–1440 (1 day).</summary>
        [Range(5, 1440, ErrorMessage = "RetryIntervalMinutes must be between 5 and 1440.")]
        public int RetryIntervalMinutes { get; set; } = 30;

        /// <summary>Whether firmware downgrade operations are permitted under this policy.</summary>
        public bool AllowDowngrade { get; set; } = false;

        /// <summary>Whether rollouts using this policy require QA verification of the firmware.</summary>
        public bool RequireQAVerification { get; set; } = true;

        /// <summary>Whether firmware approval requires two distinct approvers before deployment.</summary>
        public bool RequireDualApproval { get; set; } = false;

        /// <summary>Execution phase strategy for rollouts using this policy.</summary>
        public RolloutPhase RolloutPhase { get; set; } = RolloutPhase.Full;

        /// <summary>
        /// Canary phase percentage (1–50). Only evaluated when RolloutPhase is Canary.
        /// </summary>
        [Range(1, 50, ErrorMessage = "CanaryPercentage must be between 1 and 50.")]
        public int CanaryPercentage { get; set; } = 5;

        /// <summary>
        /// Rolling phase batch size. Only evaluated when RolloutPhase is Rolling.
        /// </summary>
        [Range(1, 10000, ErrorMessage = "RollingBatchSize must be between 1 and 10000.")]
        public int RollingBatchSize { get; set; } = 100;

        // Alias properties for service layer compatibility
        public int BatchSize { get => RollingBatchSize; set => RollingBatchSize = value; }
        public int ConcurrencyLimit { get => MaxConcurrentDevices; set => MaxConcurrentDevices = value; }
        public int RetryDelaySeconds { get => RetryIntervalMinutes * 60; set => RetryIntervalMinutes = value / 60; }
    }

    /// <summary>Request body for updating a mutable rollout policy. Only supplied fields are changed.</summary>
    public class UpdatePolicyRequest
    {
        /// <summary>Updated policy name.</summary>
        [MinLength(3)]
        [MaxLength(200)]
        public string? Name { get; set; }

        /// <summary>Updated description.</summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>Updated max concurrent devices.</summary>
        [Range(1, 10000)]
        public int? MaxConcurrentDevices { get; set; }

        /// <summary>Updated retry limit.</summary>
        [Range(0, 10)]
        public int? RetryLimit { get; set; }

        /// <summary>Updated retry interval in minutes.</summary>
        [Range(5, 1440)]
        public int? RetryIntervalMinutes { get; set; }

        /// <summary>Updated downgrade permission flag.</summary>
        public bool? AllowDowngrade { get; set; }

        /// <summary>Updated QA verification requirement.</summary>
        public bool? RequireQAVerification { get; set; }

        /// <summary>Updated dual-approval requirement.</summary>
        public bool? RequireDualApproval { get; set; }

        /// <summary>Updated rollout phase strategy.</summary>
        public RolloutPhase? RolloutPhase { get; set; }

        /// <summary>Updated canary percentage.</summary>
        [Range(1, 50)]
        public int? CanaryPercentage { get; set; }

        /// <summary>Updated rolling batch size.</summary>
        [Range(1, 10000)]
        public int? RollingBatchSize { get; set; }

        /// <summary>Active / inactive state toggle.</summary>
        public bool? IsActive { get; set; }

        // Alias properties for service layer compatibility
        public int? BatchSize { get => RollingBatchSize; set => RollingBatchSize = value; }
        public int? ConcurrencyLimit { get => MaxConcurrentDevices; set => MaxConcurrentDevices = value; }
        public int? RetryDelaySeconds { get => RetryIntervalMinutes.HasValue ? RetryIntervalMinutes * 60 : null; set => RetryIntervalMinutes = value.HasValue ? value / 60 : null; }
    }

    /// <summary>Summary row for paginated policy list responses.</summary>
    public sealed class PolicyListDto
    {
        public string PolicyId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string RolloutPhase { get; set; } = string.Empty;
        public int MaxConcurrentDevices { get; set; }
        public int RetryLimit { get; set; }
        public bool RequireQAVerification { get; set; }
        public bool RequireDualApproval { get; set; }
        public bool IsActive { get; set; }
        public int RolloutsUsingCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Full policy detail returned by GET /api/policies/{policyId}.</summary>
    public class PolicyDetailDto
    {
        public string Id { get; set; } = string.Empty;
        public string PolicyId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxConcurrentDevices { get; set; }
        public int RetryLimit { get; set; }
        public int RetryIntervalMinutes { get; set; }
        public bool AllowDowngrade { get; set; }
        public bool RequireQAVerification { get; set; }
        public bool RequireDualApproval { get; set; }
        public string RolloutPhase { get; set; } = string.Empty;
        public int CanaryPercentage { get; set; }
        public int RollingBatchSize { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty;
        public int RolloutsUsingCount { get; set; }

        // Alias properties for service layer compatibility
        public int BatchSize { get => RollingBatchSize; set => RollingBatchSize = value; }
        public int ConcurrencyLimit { get => MaxConcurrentDevices; set => MaxConcurrentDevices = value; }
        public int RetryDelaySeconds { get => RetryIntervalMinutes * 60; set => RetryIntervalMinutes = value / 60; }
    }

    /// <summary>Wrapper for paginated policy list responses.</summary>
    public sealed class PagedPolicyListResponse
    {
        public List<PolicyListDto> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public long TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    /// <summary>Alias used by IRolloutPolicyService — maps to CreatePolicyRequest.</summary>
    public class CreateRolloutPolicyRequest : CreatePolicyRequest { }

    /// <summary>Alias used by IRolloutPolicyService — maps to UpdatePolicyRequest.</summary>
    public class UpdateRolloutPolicyRequest : UpdatePolicyRequest { }

    /// <summary>Alias used by IRolloutPolicyService — maps to PolicyDetailDto.</summary>
    public class RolloutPolicyDto : PolicyDetailDto { }
}
