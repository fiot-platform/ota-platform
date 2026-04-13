using OTA.API.Models.Enums;

namespace OTA.API.Models.DTOs.OTA
{
    public class RolloutJobDto
    {
        public string JobId { get; set; } = string.Empty;
        public string RolloutId { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public string FirmwareId { get; set; } = string.Empty;
        public string FirmwareVersion { get; set; } = string.Empty;
        public OtaJobStatus Status { get; set; }
        public int RetryCount { get; set; }
        public int MaxRetries { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? LastRetriedAt { get; set; }
        public int DownloadProgressPercent { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
