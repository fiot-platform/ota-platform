namespace OTA.API.Models.DTOs
{
    // ─────────────────────────────────────────────────────────────────────────
    // Report & Dashboard DTOs
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Top-level dashboard KPI summary returned by GET /api/reports/dashboard.
    /// Contains aggregate counts and recent activity snapshots to power the main dashboard view.
    /// </summary>
    public sealed class DashboardSummaryDto
    {
        // ── Platform-wide Counts ──────────────────────────────────────────────

        /// <summary>Total number of active projects across all tenants (or scoped to the caller's tenant).</summary>
        public int TotalProjects { get; set; }

        /// <summary>Total number of registered repositories.</summary>
        public int TotalRepositories { get; set; }

        /// <summary>Total number of registered devices.</summary>
        public int TotalDevices { get; set; }

        /// <summary>Number of active devices.</summary>
        public int ActiveDevices { get; set; }

        /// <summary>Number of suspended devices.</summary>
        public int SuspendedDevices { get; set; }

        /// <summary>Number of devices that have not recently sent a heartbeat.</summary>
        public int OfflineDevices { get; set; }

        /// <summary>Total number of firmware versions.</summary>
        public int TotalFirmware { get; set; }

        /// <summary>Number of approved firmware versions.</summary>
        public int ApprovedFirmware { get; set; }

        /// <summary>Number of firmware versions pending approval.</summary>
        public int PendingApprovalFirmware { get; set; }

        /// <summary>Number of rollouts currently in Active status.</summary>
        public int ActiveRollouts { get; set; }

        /// <summary>Number of completed rollouts.</summary>
        public int CompletedRollouts { get; set; }

        /// <summary>Total number of platform users (SuperAdmin only).</summary>
        public int TotalUsers { get; set; }

        /// <summary>UTC timestamp when this summary was generated.</summary>
        public DateTime GeneratedAt { get; set; }

        // ── Aliases for frontend compatibility ────────────────────────────────

        /// <summary>Alias for TotalFirmware (frontend uses totalFirmwareVersions).</summary>
        public int TotalFirmwareVersions => TotalFirmware;

        /// <summary>Alias for PendingApprovalFirmware (frontend uses pendingApprovals).</summary>
        public int PendingApprovals => PendingApprovalFirmware;

        // ── Role-specific extras ──────────────────────────────────────────────

        /// <summary>Number of firmware versions in PendingQA status (for ReleaseManager dashboard).</summary>
        public int PendingQAFirmware { get; set; }

        /// <summary>Number of devices currently receiving an OTA update via MQTT (otaStatus = start/inprogress).</summary>
        public int DevicesUpdating { get; set; }

        /// <summary>Firmware version counts broken down by status.</summary>
        public FirmwareStatusCountsDto FirmwareCounts { get; set; } = new();

        /// <summary>Device counts broken down by status.</summary>
        public DeviceStatusCountsDto DeviceCounts { get; set; } = new();

        /// <summary>Number of rollouts currently in Active or Scheduled status.</summary>
        public int ActiveRolloutCount { get; set; }

        /// <summary>Number of rollouts in Active status right now.</summary>
        public int RunningRolloutCount { get; set; }

        // ── Recent Activity ───────────────────────────────────────────────────

        /// <summary>The 10 most recent webhook events (inbound Gitea events).</summary>
        public List<RecentWebhookEventDto> RecentWebhookEvents { get; set; } = new();

        /// <summary>The 5 most recently approved firmware versions.</summary>
        public List<RecentFirmwareApprovalDto> RecentFirmwareApprovals { get; set; } = new();

        /// <summary>The 5 most recently completed or currently active rollouts.</summary>
        public List<RecentRolloutDto> RecentRollouts { get; set; } = new();

        // ── Health Indicators ─────────────────────────────────────────────────

        /// <summary>Number of devices that have not sent a heartbeat within the inactivity threshold.</summary>
        public int InactiveDeviceCount { get; set; }

        /// <summary>Number of OTA jobs currently in Failed status requiring attention.</summary>
        public int FailedJobCount { get; set; }

        /// <summary>Number of webhook events in Failed or Retrying status.</summary>
        public int FailedWebhookEventCount { get; set; }
    }

    /// <summary>Firmware version counts per status for dashboard display.</summary>
    public sealed class FirmwareStatusCountsDto
    {
        public int Draft { get; set; }
        public int PendingQA { get; set; }
        public int QAVerified { get; set; }
        public int PendingApproval { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public int Deprecated { get; set; }
        public int Total => Draft + PendingQA + QAVerified + PendingApproval + Approved + Rejected + Deprecated;
    }

    /// <summary>Device counts per status for dashboard display.</summary>
    public sealed class DeviceStatusCountsDto
    {
        public int Active { get; set; }
        public int Inactive { get; set; }
        public int Suspended { get; set; }
        public int Decommissioned { get; set; }
        public int Total => Active + Inactive + Suspended + Decommissioned;
    }

    /// <summary>Minimal webhook event snapshot for the dashboard recent-events widget.</summary>
    public sealed class RecentWebhookEventDto
    {
        public string EventId { get; set; } = string.Empty;
        public string GiteaEventType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; }
    }

    /// <summary>Minimal firmware approval snapshot for the dashboard recent-approvals widget.</summary>
    public sealed class RecentFirmwareApprovalDto
    {
        public string FirmwareId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public DateTime ApprovedAt { get; set; }
    }

    /// <summary>Minimal rollout snapshot for the dashboard recent-rollouts widget.</summary>
    public sealed class RecentRolloutDto
    {
        public string RolloutId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int TotalDevices { get; set; }
        public int SucceededCount { get; set; }
        public DateTime? StartedAt { get; set; }
    }

    /// <summary>
    /// Data point in the firmware approval trend report.
    /// Represents the number of approvals and rejections per time bucket (day or week).
    /// </summary>
    public sealed class FirmwareApprovalTrendDto
    {
        /// <summary>The date of the time bucket (truncated to day or week start).</summary>
        public DateTime Date { get; set; }

        /// <summary>Number of firmware versions approved in this bucket.</summary>
        public int Approved { get; set; }

        /// <summary>Number of firmware versions rejected in this bucket.</summary>
        public int Rejected { get; set; }

        /// <summary>Number of firmware versions submitted (created) in this bucket.</summary>
        public int Submitted { get; set; }

        /// <summary>Average number of days from Draft to Approved in this bucket.</summary>
        public double AvgApprovalDays { get; set; }
    }

    /// <summary>
    /// Rollout success rate per project.
    /// Used to render the rollout success rate chart.
    /// </summary>
    public sealed class RolloutSuccessRateDto
    {
        /// <summary>Project identifier.</summary>
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>Project display name.</summary>
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>Total number of rollouts for the project.</summary>
        public int TotalRollouts { get; set; }

        /// <summary>Number of rollouts that completed successfully.</summary>
        public int SuccessfulRollouts { get; set; }

        /// <summary>Number of rollouts that failed.</summary>
        public int FailedRollouts { get; set; }

        /// <summary>Success rate as a percentage (0–100).</summary>
        public double SuccessRate { get; set; }
    }

    /// <summary>
    /// Aggregate device update status, optionally scoped to a customer.
    /// </summary>
    public sealed class DeviceUpdateStatusDto
    {
        /// <summary>Customer identifier this report is scoped to. Null means platform-wide.</summary>
        public string? CustomerId { get; set; }

        /// <summary>Customer display name. Null when platform-wide.</summary>
        public string? CustomerName { get; set; }

        /// <summary>Total number of devices in scope.</summary>
        public int Total { get; set; }

        /// <summary>Number of devices that are up to date (no pending/in-progress jobs).</summary>
        public int UpToDate { get; set; }

        /// <summary>Number of devices with a pending OTA update.</summary>
        public int UpdateAvailable { get; set; }

        /// <summary>Number of devices currently receiving an OTA update.</summary>
        public int Updating { get; set; }

        /// <summary>Number of devices whose last OTA update failed.</summary>
        public int Failed { get; set; }

        /// <summary>Number of devices that appear offline (no recent heartbeat).</summary>
        public int Offline { get; set; }

        /// <summary>UTC timestamp when this report was generated.</summary>
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// Rollout summary aggregated per customer tenant.
    /// Used in the customer-level reporting view and export.
    /// </summary>
    public sealed class CustomerRolloutSummaryDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public int TotalRollouts { get; set; }
        public int CompletedRollouts { get; set; }
        public int FailedRollouts { get; set; }
        public int ActiveRollouts { get; set; }
        public int TotalDevicesUpdated { get; set; }
        public int TotalDevicesFailed { get; set; }
        public double OverallSuccessRate =>
            (TotalDevicesUpdated + TotalDevicesFailed) == 0
                ? 0
                : Math.Round((double)TotalDevicesUpdated / (TotalDevicesUpdated + TotalDevicesFailed) * 100, 1);
        public DateTime? LastRolloutAt { get; set; }
    }

    /// <summary>Filter parameters for report generation endpoints.</summary>
    public sealed class ReportFilterRequest
    {
        /// <summary>Start of report date range (UTC, inclusive). Required.</summary>
        public DateTime DateFrom { get; set; } = DateTime.UtcNow.AddDays(-30);

        /// <summary>End of report date range (UTC, inclusive). Required.</summary>
        public DateTime DateTo { get; set; } = DateTime.UtcNow;

        /// <summary>Optional: scope report to a specific project.</summary>
        public string? ProjectId { get; set; }

        /// <summary>Optional: scope report to a specific customer tenant.</summary>
        public string? CustomerId { get; set; }

        /// <summary>Granularity of trend charts: "day" or "week". Defaults to "day".</summary>
        public string Granularity { get; set; } = "day";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Extended Report DTOs (9 new report pages)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>One row in the Users report table.</summary>
    public sealed class UserReportDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>One row in the Projects report table.</summary>
    public sealed class ProjectReportDto
    {
        public string ProjectId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public int RepositoryCount { get; set; }
        public int FirmwareCount { get; set; }
        public int ActiveRolloutCount { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>One row in the Repositories report table.</summary>
    public sealed class RepositoryReportDto
    {
        public string RepositoryId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public int FirmwareCount { get; set; }
        public bool WebhookConfigured { get; set; }
        public DateTime? LastSyncedAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>One row in the Firmware Versions report table.</summary>
    public sealed class FirmwareVersionReportDto
    {
        public string FirmwareId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string RepositoryId { get; set; } = string.Empty;
        public string RepositoryName { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string? ApprovedByUserId { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>One row in the Devices report table.</summary>
    public sealed class DeviceReportDto
    {
        public string DeviceId { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string ProjectId { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string? CurrentFirmwareVersion { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? LastHeartbeatAt { get; set; }
        public DateTime RegisteredAt { get; set; }
    }

    /// <summary>One firmware row in the Project → Repository → Firmware tree report.</summary>
    public sealed class ProjectRepoFirmwareRowDto
    {
        public string ProjectId { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string RepositoryId { get; set; } = string.Empty;
        public string RepositoryName { get; set; } = string.Empty;
        public string FirmwareId { get; set; } = string.Empty;
        public string FirmwareVersion { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string FirmwareStatus { get; set; } = string.Empty;
        public DateTime FirmwareCreatedAt { get; set; }
    }

    /// <summary>One OTA job row in the Device → OTA History tree report.</summary>
    public sealed class DeviceOtaHistoryRowDto
    {
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceSerial { get; set; } = string.Empty;
        public string? DeviceName { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string FirmwareVersion { get; set; } = string.Empty;
        public string JobStatus { get; set; } = string.Empty;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    /// <summary>One day's row in the Daily OTA Progress stacked-bar report.</summary>
    public sealed class DailyOtaProgressDto
    {
        public string Date { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Succeeded { get; set; }
        public int Failed { get; set; }
        public int InProgress { get; set; }
        public int Queued { get; set; }
        public int Cancelled { get; set; }
    }

    /// <summary>One stage row in the Firmware Version Stage horizontal-bar report.</summary>
    public sealed class FirmwareStageReportDto
    {
        public string Stage { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }
}
