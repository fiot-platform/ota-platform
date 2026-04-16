using System.Collections.Generic;
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
        private readonly IUserService _userService;
        private readonly ILogger<DeviceController> _logger;

        public DeviceController(IDeviceService deviceService, IUserService userService, ILogger<DeviceController> logger)
        {
            _deviceService = deviceService ?? throw new ArgumentNullException(nameof(deviceService));
            _userService   = userService ?? throw new ArgumentNullException(nameof(userService));
            _logger        = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private string CurrentUserId =>
            User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        private string CurrentEmail =>
            User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? string.Empty;

        private string ClientIp =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

        /// <summary>
        /// Public endpoint — no token required.
        /// Returns all Active (OTA-ready) devices with their current firmware version and publish topic.
        /// </summary>
        [HttpGet("ota-ready")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<List<OtaReadyDeviceDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOtaReadyDevices(CancellationToken cancellationToken = default)
        {
            var devices = await _deviceService.GetOtaReadyDevicesAsync(cancellationToken);
            return Ok(ApiResponse<List<OtaReadyDeviceDto>>.Ok(devices, $"{devices.Count} OTA-ready device(s) found."));
        }

        /// <summary>Returns a paginated list of devices. Requires CanViewDevices.</summary>
        [HttpGet]
        [Authorize(Policy = "CanViewDevices")]
        [ProducesResponseType(typeof(ApiResponse<List<DeviceDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDevices(
            [FromQuery] string? search = null,
            [FromQuery] string? projectId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            var allowedProjectIds = await GetProjectScopeAsync(cancellationToken);
            var result = await _deviceService.GetDevicesAsync(search ?? string.Empty, page, pageSize, projectId, allowedProjectIds, cancellationToken);
            var pagination = PaginationInfo.Create(page, pageSize, result.TotalCount);
            return Ok(ApiResponse<List<DeviceDto>>.Ok(result.Items, "Devices retrieved successfully.", pagination));
        }

        private async Task<List<string>?> GetProjectScopeAsync(CancellationToken cancellationToken = default)
        {
            var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? string.Empty;
            if (role is "SuperAdmin" or "PlatformAdmin") return null;

            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return new List<string>();

            var user = await _userService.GetUserByIdAsync(userId, cancellationToken);
            return user?.ProjectScope ?? new List<string>();
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
            return CreatedAtAction(nameof(GetDeviceById), new { id = created.Id },
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

        /// <summary>
        /// Registers multiple devices in a single request (bulk upload).
        /// Each row is processed independently; failures are reported without aborting the batch.
        /// Requires CanManageDevices.
        /// </summary>
        [HttpPost("bulk")]
        [Authorize(Policy = "CanManageDevices")]
        [ProducesResponseType(typeof(ApiResponse<BulkRegisterResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> BulkRegisterDevices(
            [FromBody] BulkRegisterDeviceRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<BulkRegisterResult>.Fail("Validation failed.", errors));
            }

            var result = await _deviceService.BulkRegisterDevicesAsync(
                request, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);

            var message = result.Failed == 0
                ? $"All {result.Succeeded} device(s) registered successfully."
                : $"{result.Succeeded} succeeded, {result.Failed} failed.";

            return Ok(ApiResponse<BulkRegisterResult>.Ok(result, message));
        }

        /// <summary>Reactivates a suspended device. Requires CanManageDevices.</summary>
        [HttpPost("{id}/activate")]
        [Authorize(Policy = "CanManageDevices")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ActivateDevice(string id, CancellationToken cancellationToken = default)
        {
            await _deviceService.ActivateDeviceAsync(id, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
            return Ok(ApiResponse.OkNoData("Device activated successfully."));
        }

        /// <summary>Returns paginated OTA update history for a device. Requires CanViewDevices.</summary>
        [HttpGet("{id}/ota-history")]
        [Authorize(Policy = "CanViewDevices")]
        [ProducesResponseType(typeof(ApiResponse<List<DeviceOtaHistoryItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDeviceOtaHistory(
            string id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var result = await _deviceService.GetDeviceOtaHistoryAsync(id, page, pageSize, cancellationToken);
            var pagination = PaginationInfo.Create(page, pageSize, result.TotalCount);
            return Ok(ApiResponse<List<DeviceOtaHistoryItemDto>>.Ok(result.Items, "OTA history retrieved successfully.", pagination));
        }

        /// <summary>
        /// Returns approved firmware versions compatible with a device's model.
        /// Used to populate the firmware selection dropdown when pushing firmware to a device.
        /// Requires CanViewDevices.
        /// </summary>
        [HttpGet("{id}/available-firmware")]
        [Authorize(Policy = "CanViewDevices")]
        [ProducesResponseType(typeof(ApiResponse<List<AvailableFirmwareDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAvailableFirmware(string id, CancellationToken cancellationToken = default)
        {
            var firmware = await _deviceService.GetAvailableFirmwareAsync(id, cancellationToken);
            return Ok(ApiResponse<List<AvailableFirmwareDto>>.Ok(firmware, $"{firmware.Count} firmware version(s) available."));
        }

        /// <summary>
        /// Pushes a specific firmware version to a device by creating a direct OTA job.
        /// The device will receive this firmware on its next check-update request.
        /// Requires CanManageDevices.
        /// </summary>
        [HttpPost("{id}/push-firmware")]
        [Authorize(Policy = "CanManageDevices")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PushFirmware(
            string id,
            [FromBody] PushFirmwareRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.Fail("Validation failed.", errors));
            }

            var jobId = await _deviceService.PushFirmwareToDeviceAsync(
                id, request.FirmwareVersionId, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
            return Ok(ApiResponse<object>.Ok(new { jobId }, "Firmware push queued successfully."));
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
