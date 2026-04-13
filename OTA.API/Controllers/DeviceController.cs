using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OTA.API.Helpers;
using OTA.API.Models.DTOs;
using OTA.API.Services.Interfaces;

namespace OTA.API.Controllers
{
    /// <summary>
    /// Device management endpoints covering registration, heartbeat ingestion, update eligibility checks,
    /// status reporting, and administrative lifecycle operations (suspend / decommission).
    /// </summary>
    [ApiController]
    [Route("api/devices")]
    [Authorize]
    [Produces("application/json")]
    public class DeviceController : ControllerBase
    {
        private readonly IDeviceService _deviceService;
        private readonly ILogger<DeviceController> _logger;

        public DeviceController(IDeviceService deviceService, ILogger<DeviceController> logger)
        {
            _deviceService = deviceService ?? throw new ArgumentNullException(nameof(deviceService));
            _logger        = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private string CurrentUserId =>
            User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        private string CurrentEmail =>
            User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? string.Empty;

        private string ClientIp =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

        /// <summary>Returns a paginated list of devices. Requires CanViewDevices.</summary>
        [HttpGet]
        [Authorize(Policy = "CanViewDevices")]
        [ProducesResponseType(typeof(ApiResponse<List<DeviceDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDevices(
            [FromQuery] string? filter = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            var result = await _deviceService.GetDevicesAsync(filter ?? string.Empty, page, pageSize, cancellationToken);
            var pagination = PaginationInfo.Create(page, pageSize, result.TotalCount);
            return Ok(ApiResponse<List<DeviceDto>>.Ok(result.Items, "Devices retrieved successfully.", pagination));
        }

        /// <summary>Returns a single device by its identifier. Requires CanViewDevices.</summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "CanViewDevices")]
        [ProducesResponseType(typeof(ApiResponse<DeviceDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDeviceById(string id, CancellationToken cancellationToken = default)
        {
            var device = await _deviceService.GetDeviceByIdAsync(id, cancellationToken);
            if (device == null)
                return NotFound(ApiResponse<DeviceDto>.Fail($"Device '{id}' was not found."));

            return Ok(ApiResponse<DeviceDto>.Ok(device));
        }

        /// <summary>Registers a new device. Requires CanManageDevices.</summary>
        [HttpPost]
        [Authorize(Policy = "CanManageDevices")]
        [ProducesResponseType(typeof(ApiResponse<DeviceDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterDevice(
            [FromBody] RegisterDeviceRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<DeviceDto>.Fail("Validation failed.", errors));
            }

            var created = await _deviceService.RegisterDeviceAsync(request, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
            return CreatedAtAction(nameof(GetDeviceById), new { id = created.DeviceId },
                ApiResponse<DeviceDto>.Ok(created, "Device registered successfully."));
        }

        /// <summary>Updates a device's mutable metadata. Requires CanManageDevices.</summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "CanManageDevices")]
        [ProducesResponseType(typeof(ApiResponse<DeviceDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateDevice(
            string id,
            [FromBody] UpdateDeviceRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<DeviceDto>.Fail("Validation failed.", errors));
            }

            var updated = await _deviceService.UpdateDeviceAsync(id, request, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
            return Ok(ApiResponse<DeviceDto>.Ok(updated, "Device updated successfully."));
        }

        /// <summary>
        /// Receives a heartbeat from a device. Accepts the Device role JWT.
        /// Updates the last-seen timestamp and current firmware version.
        /// </summary>
        [HttpPost("heartbeat")]
        [Authorize(Roles = "Device")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Heartbeat(
            [FromBody] DeviceHeartbeatRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.Fail("Validation failed.", errors));
            }

            var deviceId = CurrentUserId;
            await _deviceService.ProcessHeartbeatAsync(deviceId, request, cancellationToken);
            return Ok(ApiResponse.OkNoData("Heartbeat received."));
        }

        /// <summary>
        /// Checks whether a firmware update is available for the calling device.
        /// Returns download URL, hash, and mandatory flag when an update is available.
        /// </summary>
        [HttpPost("check-update")]
        [Authorize(Roles = "Device")]
        [ProducesResponseType(typeof(ApiResponse<CheckUpdateResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CheckForUpdate(
            [FromBody] CheckUpdateRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<CheckUpdateResponse>.Fail("Validation failed.", errors));
            }

            var response = await _deviceService.CheckForUpdateAsync(request, cancellationToken);
            return Ok(ApiResponse<CheckUpdateResponse>.Ok(response, response.HasUpdate ? "Update available." : "Device is up to date."));
        }

        /// <summary>
        /// Records the outcome of an OTA update job reported by a device.
        /// Updates the job status and the device's current firmware version on success.
        /// </summary>
        [HttpPost("report-status")]
        [Authorize(Roles = "Device")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReportStatus(
            [FromBody] ReportStatusRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.Fail("Validation failed.", errors));
            }

            await _deviceService.ReportStatusAsync(request, cancellationToken);
            return Ok(ApiResponse.OkNoData("Status report received."));
        }

        /// <summary>Suspends a device. Requires CanManageDevices.</summary>
        [HttpPost("{id}/suspend")]
        [Authorize(Policy = "CanManageDevices")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SuspendDevice(string id, CancellationToken cancellationToken = default)
        {
            await _deviceService.SuspendDeviceAsync(id, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
            return Ok(ApiResponse.OkNoData("Device suspended successfully."));
        }

        /// <summary>Decommissions a device permanently. SuperAdmin and PlatformAdmin only.</summary>
        [HttpPost("{id}/decommission")]
        [Authorize(Roles = "SuperAdmin,PlatformAdmin")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DecommissionDevice(string id, CancellationToken cancellationToken = default)
        {
            await _deviceService.DecommissionDeviceAsync(id, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
            return Ok(ApiResponse.OkNoData("Device decommissioned successfully."));
        }
    }
}
