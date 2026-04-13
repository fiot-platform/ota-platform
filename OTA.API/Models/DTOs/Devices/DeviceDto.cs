using OTA.API.Models.Enums;

namespace OTA.API.Models.DTOs.Devices
{
    public class DeviceDto
    {
        public string DeviceId { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string? HardwareRevision { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? SiteId { get; set; }
        public string? SiteName { get; set; }
        public DeviceStatus Status { get; set; }
        public string? CurrentFirmwareVersion { get; set; }
        public string? PreviousFirmwareVersion { get; set; }
        public DateTime? LastHeartbeatAt { get; set; }
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, string> Metadata { get; set; } = new();
        public DateTime RegisteredAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class HeartbeatRequest
    {
        public string? CurrentFirmwareVersion { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    public class CheckUpdateRequest
    {
        public string CurrentFirmwareVersion { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string? HardwareRevision { get; set; }
        public string? Channel { get; set; }
    }

    public class CheckUpdateResponse
    {
        public bool UpdateAvailable { get; set; }
        public string? NewVersion { get; set; }
        public string? DownloadUrl { get; set; }
        public string? FileSha256 { get; set; }
        public long FileSizeBytes { get; set; }
        public bool IsMandatory { get; set; }
        public string? ReleaseNotes { get; set; }
        public string? FirmwareId { get; set; }
    }

    public class ReportStatusRequest
    {
        public string JobId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int? DownloadProgressPercent { get; set; }
        public string? ErrorMessage { get; set; }
        public string? InstalledVersion { get; set; }
    }

    public class UpdateDeviceRequest
    {
        public string? SiteId { get; set; }
        public string? SiteName { get; set; }
        public List<string>? Tags { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }

    public class SuspendDeviceRequest
    {
        public string? Reason { get; set; }
    }
}
