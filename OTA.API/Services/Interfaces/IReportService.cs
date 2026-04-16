using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OTA.API.Models.DTOs;
using OTA.API.Models.Enums;

namespace OTA.API.Services.Interfaces
{
    /// <summary>
    /// Service interface for dashboard and reporting aggregations across the OTA platform.
    /// </summary>
    public interface IReportService
    {
        /// <summary>
        /// Returns a high-level dashboard summary of device counts, firmware status, and rollout activity.
        /// CustomerAdmin callers are automatically scoped to their customerId.
        /// </summary>
        /// <param name="userId">The identifier of the requesting user.</param>
        /// <param name="role">The role of the requesting user for scoping logic.</param>
        /// <param name="customerId">The customer identifier used to scope results for CustomerAdmin callers.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="DashboardSummaryDto"/> containing aggregated counts and status breakdowns.</returns>
        Task<DashboardSummaryDto> GetDashboardSummaryAsync(string userId, UserRole role, string? customerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a time-series trend of firmware approvals over the specified number of days for charting.
        /// </summary>
        /// <param name="days">The lookback window in days (e.g. 30 for last 30 days).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of daily firmware approval counts.</returns>
        Task<List<FirmwareApprovalTrendDto>> GetFirmwareApprovalTrendAsync(int days, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns rollout success rates per project. When projectId is null, returns rates for all projects.
        /// </summary>
        /// <param name="projectId">Optional project identifier. Null returns all projects.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<List<RolloutSuccessRateDto>> GetRolloutSuccessRateAsync(string? projectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns device update status. When customerId is null (SuperAdmin), returns platform-wide stats.
        /// </summary>
        /// <param name="customerId">Optional customer identifier. Null returns platform-wide data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<List<DeviceUpdateStatusDto>> GetDeviceUpdateStatusAsync(string? customerId, CancellationToken cancellationToken = default);

        // ── Extended report methods ────────────────────────────────────────────

        /// <summary>Returns a flat list of all platform users for the Users report.</summary>
        Task<List<UserReportDto>> GetUsersReportAsync(CancellationToken cancellationToken = default);

        /// <summary>Returns a flat list of all projects with repo/firmware/rollout counts for the Projects report.</summary>
        Task<List<ProjectReportDto>> GetProjectsReportAsync(CancellationToken cancellationToken = default);

        /// <summary>Returns a flat list of all repositories for the Repositories report.</summary>
        Task<List<RepositoryReportDto>> GetRepositoriesReportAsync(CancellationToken cancellationToken = default);

        /// <summary>Returns a flat list of all firmware versions for the Firmware Versions report.</summary>
        Task<List<FirmwareVersionReportDto>> GetFirmwareVersionsReportAsync(CancellationToken cancellationToken = default);

        /// <summary>Returns a flat list of all devices for the Devices report.</summary>
        Task<List<DeviceReportDto>> GetDevicesReportAsync(CancellationToken cancellationToken = default);

        /// <summary>Returns flat rows for the Project → Repository → Firmware tree report.</summary>
        Task<List<ProjectRepoFirmwareRowDto>> GetProjectRepoFirmwareReportAsync(CancellationToken cancellationToken = default);

        /// <summary>Returns flat rows for the Device → OTA Job History tree report.</summary>
        /// <param name="deviceId">Optional device ID filter. Null returns all devices.</param>
        Task<List<DeviceOtaHistoryRowDto>> GetDeviceOtaHistoryAsync(string? deviceId, CancellationToken cancellationToken = default);

        /// <summary>Returns one row per day over the last <paramref name="days"/> days with OTA job outcome counts.</summary>
        Task<List<DailyOtaProgressDto>> GetDailyOtaProgressAsync(int days, CancellationToken cancellationToken = default);

        /// <summary>Returns one row per firmware lifecycle stage with count and percentage.</summary>
        Task<List<FirmwareStageReportDto>> GetFirmwareStageReportAsync(CancellationToken cancellationToken = default);
    }
}
