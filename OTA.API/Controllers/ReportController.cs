using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OTA.API.Helpers;
using OTA.API.Models.DTOs;
using OTA.API.Models.Enums;
using OTA.API.Services.Interfaces;

namespace OTA.API.Controllers
{
    /// <summary>
    /// Reporting and analytics endpoints. Dashboard, firmware approval trends, rollout success rates,
    /// and device update status. All endpoints require the CanViewReports policy.
    /// CustomerAdmin callers are automatically scoped to their own customer by the service layer.
    /// </summary>
    [ApiController]
    [Route("api/reports")]
    [Authorize(Policy = "CanViewReports")]
    [Produces("application/json")]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly ILogger<ReportController> _logger;

        public ReportController(IReportService reportService, ILogger<ReportController> logger)
        {
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            _logger        = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private string CurrentUserId =>
            User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        private string? CurrentCustomerId =>
            User.FindFirstValue("customerId");

        private UserRole CurrentRole
        {
            get
            {
                var roleClaim = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? string.Empty;
                return Enum.TryParse<UserRole>(roleClaim, ignoreCase: true, out var role) ? role : UserRole.Viewer;
            }
        }

        /// <summary>
        /// Returns a high-level platform dashboard summary: device counts, firmware status breakdown,
        /// rollout activity, and recent events. CustomerAdmin results are automatically scoped to their customer.
        /// </summary>
        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(ApiResponse<DashboardSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken = default)
        {
            var summary = await _reportService.GetDashboardSummaryAsync(
                CurrentUserId, CurrentRole, CurrentCustomerId, cancellationToken);

            return Ok(ApiResponse<DashboardSummaryDto>.Ok(summary, "Dashboard summary retrieved."));
        }

        /// <summary>
        /// Returns a daily time-series of firmware approvals over the specified lookback window.
        /// Suitable for charting approval throughput trends.
        /// </summary>
        [HttpGet("firmware-approval-trend")]
        [ProducesResponseType(typeof(ApiResponse<List<FirmwareApprovalTrendDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetFirmwareApprovalTrend(
            [FromQuery] int days = 30,
            CancellationToken cancellationToken = default)
        {
            if (days < 1 || days > 365)
                return BadRequest(ApiResponse<object>.Fail("Parameter 'days' must be between 1 and 365."));

            var trend = await _reportService.GetFirmwareApprovalTrendAsync(days, cancellationToken);
            return Ok(ApiResponse<List<FirmwareApprovalTrendDto>>.Ok(trend, $"Firmware approval trend for last {days} day(s)."));
        }

        /// <summary>
        /// Returns success rate and failure breakdown for all rollouts in the specified project.
        /// </summary>
        [HttpGet("rollout-success-rate")]
        [ProducesResponseType(typeof(ApiResponse<List<RolloutSuccessRateDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRolloutSuccessRate(
            [FromQuery] string? projectId = null,
            CancellationToken cancellationToken = default)
        {
            var rates = await _reportService.GetRolloutSuccessRateAsync(projectId, cancellationToken);
            return Ok(ApiResponse<List<RolloutSuccessRateDto>>.Ok(rates, "Rollout success rate retrieved."));
        }

        /// <summary>
        /// Returns a breakdown of device firmware update status (up-to-date, pending, failed)
        /// for the specified customer.
        /// </summary>
        [HttpGet("device-update-status")]
        [ProducesResponseType(typeof(ApiResponse<List<DeviceUpdateStatusDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDeviceUpdateStatus(
            [FromQuery] string? customerId = null,
            CancellationToken cancellationToken = default)
        {
            // CustomerAdmin is always scoped to their own customer
            var effectiveCustomerId = CurrentRole == UserRole.CustomerAdmin
                ? CurrentCustomerId
                : customerId;

            var statuses = await _reportService.GetDeviceUpdateStatusAsync(effectiveCustomerId, cancellationToken);
            return Ok(ApiResponse<List<DeviceUpdateStatusDto>>.Ok(statuses, "Device update status retrieved."));
        }

        // ── Extended report endpoints ─────────────────────────────────────────

        /// <summary>Returns a flat list of all platform users for the Users report.</summary>
        [HttpGet("users")]
        [ProducesResponseType(typeof(ApiResponse<List<UserReportDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsersReport(CancellationToken cancellationToken = default)
        {
            var data = await _reportService.GetUsersReportAsync(cancellationToken);
            return Ok(ApiResponse<List<UserReportDto>>.Ok(data, "Users report retrieved."));
        }

        /// <summary>Returns a flat list of all projects with aggregate counts for the Projects report.</summary>
        [HttpGet("projects")]
        [ProducesResponseType(typeof(ApiResponse<List<ProjectReportDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProjectsReport(CancellationToken cancellationToken = default)
        {
            var data = await _reportService.GetProjectsReportAsync(cancellationToken);
            return Ok(ApiResponse<List<ProjectReportDto>>.Ok(data, "Projects report retrieved."));
        }

        /// <summary>Returns a flat list of all repositories for the Repositories report.</summary>
        [HttpGet("repositories")]
        [ProducesResponseType(typeof(ApiResponse<List<RepositoryReportDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRepositoriesReport(CancellationToken cancellationToken = default)
        {
            var data = await _reportService.GetRepositoriesReportAsync(cancellationToken);
            return Ok(ApiResponse<List<RepositoryReportDto>>.Ok(data, "Repositories report retrieved."));
        }

        /// <summary>Returns a flat list of all firmware versions for the Firmware Versions report.</summary>
        [HttpGet("firmware-versions")]
        [ProducesResponseType(typeof(ApiResponse<List<FirmwareVersionReportDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFirmwareVersionsReport(CancellationToken cancellationToken = default)
        {
            var data = await _reportService.GetFirmwareVersionsReportAsync(cancellationToken);
            return Ok(ApiResponse<List<FirmwareVersionReportDto>>.Ok(data, "Firmware versions report retrieved."));
        }

        /// <summary>Returns a flat list of all devices for the Devices report.</summary>
        [HttpGet("devices")]
        [ProducesResponseType(typeof(ApiResponse<List<DeviceReportDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDevicesReport(CancellationToken cancellationToken = default)
        {
            var data = await _reportService.GetDevicesReportAsync(cancellationToken);
            return Ok(ApiResponse<List<DeviceReportDto>>.Ok(data, "Devices report retrieved."));
        }

        /// <summary>Returns flat rows for the Project → Repository → Firmware tree report.</summary>
        [HttpGet("project-repo-firmware")]
        [ProducesResponseType(typeof(ApiResponse<List<ProjectRepoFirmwareRowDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProjectRepoFirmwareReport(CancellationToken cancellationToken = default)
        {
            var data = await _reportService.GetProjectRepoFirmwareReportAsync(cancellationToken);
            return Ok(ApiResponse<List<ProjectRepoFirmwareRowDto>>.Ok(data, "Project-repo-firmware report retrieved."));
        }

        /// <summary>Returns flat rows for the Device → OTA Job History tree report.</summary>
        [HttpGet("device-ota-history")]
        [ProducesResponseType(typeof(ApiResponse<List<DeviceOtaHistoryRowDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDeviceOtaHistory(
            [FromQuery] string? deviceId = null,
            CancellationToken cancellationToken = default)
        {
            var data = await _reportService.GetDeviceOtaHistoryAsync(deviceId, cancellationToken);
            return Ok(ApiResponse<List<DeviceOtaHistoryRowDto>>.Ok(data, "Device OTA history retrieved."));
        }

        /// <summary>
        /// Returns one row per day over the last N days with OTA job outcome counts.
        /// </summary>
        [HttpGet("daily-ota-progress")]
        [ProducesResponseType(typeof(ApiResponse<List<DailyOtaProgressDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetDailyOtaProgress(
            [FromQuery] int days = 14,
            CancellationToken cancellationToken = default)
        {
            if (days < 1 || days > 90)
                return BadRequest(ApiResponse<object>.Fail("Parameter 'days' must be between 1 and 90."));

            var data = await _reportService.GetDailyOtaProgressAsync(days, cancellationToken);
            return Ok(ApiResponse<List<DailyOtaProgressDto>>.Ok(data, $"Daily OTA progress for last {days} day(s) retrieved."));
        }

        /// <summary>Returns one row per firmware lifecycle stage with count and percentage.</summary>
        [HttpGet("firmware-stage")]
        [ProducesResponseType(typeof(ApiResponse<List<FirmwareStageReportDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFirmwareStageReport(CancellationToken cancellationToken = default)
        {
            var data = await _reportService.GetFirmwareStageReportAsync(cancellationToken);
            return Ok(ApiResponse<List<FirmwareStageReportDto>>.Ok(data, "Firmware stage report retrieved."));
        }
    }
}
