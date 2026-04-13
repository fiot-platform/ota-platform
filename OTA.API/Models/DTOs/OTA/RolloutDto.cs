using OTA.API.Models.Enums;

namespace OTA.API.Models.DTOs.OTA
{
    public class RolloutDto
    {
        public string RolloutId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ProjectId { get; set; } = string.Empty;
        public string FirmwareId { get; set; } = string.Empty;
        public string FirmwareVersion { get; set; } = string.Empty;
        public RolloutStatus Status { get; set; }
        public TargetType TargetType { get; set; }
        public List<string> TargetIds { get; set; } = new();
        public string? Channel { get; set; }
        public RolloutPhase Phase { get; set; }
        public int TotalDevices { get; set; }
        public int SucceededCount { get; set; }
        public int FailedCount { get; set; }
        public int CancelledCount { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? PausedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class RolloutSummaryDto
    {
        public string RolloutId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public RolloutStatus Status { get; set; }
        public int TotalDevices { get; set; }
        public int SucceededCount { get; set; }
        public int FailedCount { get; set; }
        public int CancelledCount { get; set; }
        public double SuccessRatePercent { get; set; }
        public double ProgressPercent { get; set; }
        public TimeSpan? Duration { get; set; }
    }

    public class CancelRolloutRequest
    {
        public string? Reason { get; set; }
    }
}
