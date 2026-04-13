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
    }
}
