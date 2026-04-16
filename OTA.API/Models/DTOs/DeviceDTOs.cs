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
        /// <summary>Project the device belongs to.</summary>
        [Required(ErrorMessage = "ProjectName is required.")]
        [MaxLength(200)]
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>Client / customer name that owns this device.</summary>
        [Required(ErrorMessage = "CustomerCode is required.")]
        [MaxLength(200)]
        public string CustomerCode { get; set; } = string.Empty;

        /// <summary>
        /// MAC address, IMEI, or IP address — used as the unique device identifier.
        /// </summary>
        [Required(ErrorMessage = "MacImeiIp is required.")]
        [MaxLength(100)]
        public string MacImeiIp { get; set; } = string.Empty;

        /// <summary>Device model identifier (e.g., "EDGE-GW-V2").</summary>
        [Required(ErrorMessage = "Model is required.")]
        [MaxLength(100)]
        public string Model { get; set; } = string.Empty;

        /// <summary>Firmware version currently running on the device at registration time.</summary>
        [MaxLength(50)]
        public string? CurrentFirmwareVersion { get; set; }

        /// <summary>
        /// Custom MQTT publish topic for this device's registration event.
        /// Defaults to OTA/{MacImeiIp}/Status when left blank.
        /// </summary>
        [MaxLength(200)]
        public string? PublishTopic { get; set; }
    }

    /// <summary>Request body for updating mutable device attributes.</summary>
    public sealed class UpdateDeviceRequest
    {
        /// <summary>Updated device model identifier.</summary>
        [MaxLength(100)]
        public string? Model { get; set; }

        /// <summary>Updated current firmware version (manual override).</summary>
        [MaxLength(50)]
        public string? CurrentFirmwareVersion { get; set; }

        /// <summary>
        /// Updated MQTT publish topic for this device.
        /// When provided, the platform re-publishes the device info to this topic.
        /// </summary>
        [MaxLength(200)]
        public string? PublishTopic { get; set; }
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
        /// <summary>MongoDB ObjectId — used as the route key in /api/devices/{id}.</summary>
        public string Id { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        /// <summary>MAC address, IMEI, or IP used as the device identifier.</summary>
        public string? MacImeiIp { get; set; }
        /// <summary>Project the device belongs to.</summary>
        public string? ProjectName { get; set; }
        public string Model { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? CurrentFirmwareVersion { get; set; }
        public string? PreviousFirmwareVersion { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? LastHeartbeatAt { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? RegisteredByUserId { get; set; }
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, string> Metadata { get; set; } = new();

        /// <summary>
        /// MQTT topic used to publish registration / update events for this device.
        /// Defaults to OTA/{SerialNumber}/Status when not explicitly set.
        /// </summary>
        public string? PublishTopic { get; set; }

        // ── Live OTA progress (updated via MQTT status packets) ───────────────
        /// <summary>Last OTA status reported by the device (start | inprogress | success | failed | rollback).</summary>
        public string? OtaStatus { get; set; }
        /// <summary>Download/install progress percentage 0–100.</summary>
        public int OtaProgress { get; set; }
        /// <summary>Firmware version the device is updating to.</summary>
        public string? OtaTargetVersion { get; set; }
        /// <summary>UTC timestamp of the last OTA status packet.</summary>
        public DateTime? OtaUpdatedAt { get; set; }
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

    /// <summary>Request body for registering multiple devices in a single call.</summary>
    public sealed class BulkRegisterDeviceRequest
    {
        /// <summary>List of devices to register. Must contain at least one entry.</summary>
        [Required]
        [MinLength(1, ErrorMessage = "At least one device is required.")]
        public List<RegisterDeviceRequest> Devices { get; set; } = new();
    }

    /// <summary>Per-row error detail returned when a single device in a bulk request fails.</summary>
    public sealed class BulkRegisterError
    {
        /// <summary>1-based row number in the uploaded file.</summary>
        public int Row { get; set; }
        /// <summary>MAC/IMEI/IP used to identify the failing row.</summary>
        public string Identifier { get; set; } = string.Empty;
        /// <summary>Human-readable error message.</summary>
        public string Error { get; set; } = string.Empty;
    }

    /// <summary>Summary result returned by the bulk-register endpoint.</summary>
    public sealed class BulkRegisterResult
    {
        public int Total { get; set; }
        public int Succeeded { get; set; }
        public int Failed { get; set; }
        public List<BulkRegisterError> Errors { get; set; } = new();
    }

    /// <summary>Request body to push a specific firmware version to a device.</summary>
    public sealed class PushFirmwareRequest
    {
        /// <summary>The MongoDB ObjectId of the firmware version to push.</summary>
        [Required(ErrorMessage = "FirmwareVersionId is required.")]
        public string FirmwareVersionId { get; set; } = string.Empty;
    }

    /// <summary>Available firmware version that can be pushed to a specific device.</summary>
    public sealed class AvailableFirmwareDto
    {
        public string Id { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ReleaseNotes { get; set; }
        public bool IsMandate { get; set; }
        public string? DownloadUrl { get; set; }
        public long FileSizeBytes { get; set; }
        public string? ApprovedAt { get; set; }
        public List<string> SupportedModels { get; set; } = new();
    }

    /// <summary>Single entry in a device's OTA update history.</summary>
    public sealed class DeviceOtaHistoryItemDto
    {
        public string Id { get; set; } = string.Empty;
        /// <summary>Rollout job ID — only set for Rollout-sourced entries.</summary>
        public string? RolloutId { get; set; }
        /// <summary>Human-readable rollout name — only set for Rollout-sourced entries.</summary>
        public string? RolloutName { get; set; }
        public string FirmwareVersion { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        /// <summary>Download/install progress percentage 0–100.</summary>
        public int Progress { get; set; }
        /// <summary>"MQTT" for device-initiated events, "Rollout" for platform-managed jobs.</summary>
        public string Source { get; set; } = "Rollout";
        public DateTime? CompletedAt { get; set; }
        /// <summary>When this event was recorded (used for sorting and display).</summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>Lightweight device record returned by the public OTA-ready endpoint (no auth required).</summary>
    public sealed class OtaReadyDeviceDto
    {
        public string DeviceId { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string MacImeiIp { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CurrentFirmwareVersion { get; set; } = string.Empty;
        public string PublishTopic { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? LastHeartbeatAt { get; set; }
        public DateTime RegisteredAt { get; set; }

        // ── OTA Status ────────────────────────────────────────────────────────
        /// <summary>Last OTA lifecycle status reported by the device (start | inprogress | success | failed | rollback). Null if no OTA attempted.</summary>
        public string? OtaStatus { get; set; }
        /// <summary>Download/install progress percentage 0–100.</summary>
        public int OtaProgress { get; set; }
        /// <summary>Firmware version the device is currently updating to.</summary>
        public string? OtaTargetVersion { get; set; }
        /// <summary>UTC timestamp of the last OTA status packet received from the device.</summary>
        public DateTime? OtaUpdatedAt { get; set; }
    }
}
