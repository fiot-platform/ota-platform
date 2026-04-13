using System.ComponentModel.DataAnnotations;
using OTA.API.Models.Enums;

namespace OTA.API.Models.DTOs
{
    // ─────────────────────────────────────────────────────────────────────────
    // Device DTOs
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Request body for registering a new IoT device with the platform.</summary>
    public sealed class RegisterDeviceRequest
    {
        /// <summary>Manufacturer serial number; must be globally unique within the platform.</summary>
        [Required(ErrorMessage = "SerialNumber is required.")]
        [MaxLength(100)]
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>Device model identifier (e.g., "EDGE-GW-V2").</summary>
        [Required(ErrorMessage = "Model is required.")]
        [MaxLength(100)]
        public string Model { get; set; } = string.Empty;

        /// <summary>Hardware revision string (e.g., "REV-B").</summary>
        [MaxLength(50)]
        public string? HardwareRevision { get; set; }

        /// <summary>Customer tenant identifier that owns this device.</summary>
        [Required(ErrorMessage = "CustomerId is required.")]
        [MaxLength(36)]
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>Display name of the customer (for denormalisation).</summary>
        [Required(ErrorMessage = "CustomerName is required.")]
        [MaxLength(200)]
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>Site or location identifier where the device is deployed.</summary>
        [MaxLength(36)]
        public string? SiteId { get; set; }

        /// <summary>Display name of the site (for denormalisation).</summary>
        [MaxLength(200)]
        public string? SiteName { get; set; }

        /// <summary>Initial firmware version currently running on the device at registration time.</summary>
        [MaxLength(50)]
        public string? CurrentFirmwareVersion { get; set; }

        /// <summary>Categorisation tags (e.g., ["warehouse-zone-a"]).</summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>Initial device metadata key-value pairs.</summary>
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>Request body for updating mutable device attributes.</summary>
    public sealed class UpdateDeviceRequest
    {
        /// <summary>Updated site identifier.</summary>
        [MaxLength(36)]
        public string? SiteId { get; set; }

        /// <summary>Updated site display name.</summary>
        [MaxLength(200)]
        public string? SiteName { get; set; }

        /// <summary>Replacement tags list.</summary>
        public List<string>? Tags { get; set; }

        /// <summary>Replacement metadata dictionary.</summary>
        public Dictionary<string, string>? Metadata { get; set; }

        /// <summary>Updated device status (e.g., to suspend or reactivate).</summary>
        public DeviceStatus? Status { get; set; }
    }

    /// <summary>Request body sent by a device to report its current state (heartbeat).</summary>
    public sealed class DeviceHeartbeatRequest
    {
        /// <summary>Firmware version currently running on the device.</summary>
        [Required]
        [MaxLength(50)]
        public string CurrentFirmwareVersion { get; set; } = string.Empty;

        /// <summary>Current device metadata snapshot (OS version, chip ID, uptime, etc.).</summary>
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>Request body sent by a device to check whether a firmware update is available.</summary>
    public sealed class CheckUpdateRequest
    {
        /// <summary>Platform DeviceId of the calling device (from JWT claim).</summary>
        [Required]
        [MaxLength(36)]
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>Firmware version currently installed on the device.</summary>
        [Required]
        [MaxLength(50)]
        public string CurrentVersion { get; set; } = string.Empty;

        /// <summary>Device model identifier for compatibility matching.</summary>
        [Required]
        [MaxLength(100)]
        public string Model { get; set; } = string.Empty;

        /// <summary>Hardware revision identifier for compatibility matching.</summary>
        [MaxLength(50)]
        public string? HardwareRevision { get; set; }
    }

    /// <summary>Response body returned by the check-update endpoint.</summary>
    public sealed class CheckUpdateResponse
    {
        /// <summary>Whether a firmware update is available and has been dispatched for this device.</summary>
        public bool HasUpdate { get; set; }

        /// <summary>The update job identifier; null when HasUpdate is false.</summary>
        public string? JobId { get; set; }

        /// <summary>Firmware version string to be installed; null when HasUpdate is false.</summary>
        public string? FirmwareVersion { get; set; }

        /// <summary>Direct download URL for the firmware binary; null when HasUpdate is false.</summary>
        public string? DownloadUrl { get; set; }

        /// <summary>SHA-256 hex digest the device should verify after download; null when HasUpdate is false.</summary>
        public string? Sha256 { get; set; }

        /// <summary>File size in bytes; null when HasUpdate is false.</summary>
        public long? FileSizeBytes { get; set; }

        /// <summary>Whether the update must be applied immediately (no deferral permitted).</summary>
        public bool IsMandatory { get; set; }

        /// <summary>Optional release notes for display on device UI (if applicable).</summary>
        public string? ReleaseNotes { get; set; }
    }

    /// <summary>Request body sent by a device to report the status of an OTA job.</summary>
    public sealed class ReportStatusRequest
    {
        /// <summary>The OTA job identifier returned in the check-update response.</summary>
        [Required(ErrorMessage = "JobId is required.")]
        [MaxLength(36)]
        public string JobId { get; set; } = string.Empty;

        /// <summary>New job status reported by the device.</summary>
        [Required]
        public OtaJobStatus Status { get; set; }

        /// <summary>Error message reported by the device when Status is Failed.</summary>
        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }
    }

    /// <summary>Summary row for paginated device list responses.</summary>
    public sealed class DeviceListDto
    {
        public string DeviceId { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string? HardwareRevision { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? SiteId { get; set; }
        public string? SiteName { get; set; }
        public string? CurrentFirmwareVersion { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? LastHeartbeatAt { get; set; }
        public DateTime RegisteredAt { get; set; }
    }

    /// <summary>Full device detail returned by GET /api/devices/{deviceId}.</summary>
    public class DeviceDetailDto
    {
        public string DeviceId { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string? HardwareRevision { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? SiteId { get; set; }
        public string? SiteName { get; set; }
        public string? CurrentFirmwareVersion { get; set; }
        public string? PreviousFirmwareVersion { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? LastHeartbeatAt { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? RegisteredByUserId { get; set; }
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    /// <summary>Wrapper for paginated device list responses.</summary>
    public sealed class PagedDeviceListResponse
    {
        public List<DeviceListDto> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public long TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
