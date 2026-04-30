using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OTA.API.Models.DTOs;

namespace OTA.API.Services.Interfaces
{
    /// <summary>
    /// Service interface for device registration, heartbeat processing, and OTA update eligibility checks.
    /// </summary>
    public interface IDeviceService
    {
        /// <summary>
        /// Registers a new device in the platform, validates uniqueness of serial number, and logs the audit event.
        /// </summary>
        /// <param name="request">The device registration request including serial number, model, hardware revision, and customer details.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The registered device DTO.</returns>
        Task<DeviceDto> RegisterDeviceAsync(RegisterDeviceRequest request, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates mutable metadata fields of a registered device and logs the change.
        /// </summary>
        /// <param name="deviceId">The identifier of the device to update.</param>
        /// <param name="request">The update request payload.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated device DTO.</returns>
        Task<DeviceDto> UpdateDeviceAsync(string deviceId, UpdateDeviceRequest request, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes a heartbeat signal from a device, updating its last-seen timestamp and optionally its reported firmware version.
        /// </summary>
        /// <param name="request">The heartbeat request containing device identifier and current firmware version.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ProcessHeartbeatAsync(string deviceId, DeviceHeartbeatRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines whether an approved firmware update is available for the device based on model,
        /// hardware revision, channel, and version comparison.
        /// </summary>
        /// <param name="request">The check-update request containing device identifier, current version, and preferred channel.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="CheckUpdateResponse"/> indicating whether an update is available and the target firmware details.</returns>
        Task<CheckUpdateResponse> CheckForUpdateAsync(CheckUpdateRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Records a status report from a device following a firmware update attempt, updating the OTA job and device state.
        /// </summary>
        /// <param name="request">The status report request containing job identifier, outcome, and firmware version installed.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ReportStatusAsync(ReportStatusRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a device by its internal identifier.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The device DTO, or null if not found.</returns>
        Task<DeviceDto?> GetDeviceByIdAsync(string deviceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a paginated list of devices with optional text filter.
        /// </summary>
        /// <param name="filter">Optional text filter matching serial number or model.</param>
        /// <param name="page">One-based page number.</param>
        /// <param name="pageSize">Number of results per page.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Paged result containing device DTOs and total count.</returns>
        Task<PagedResult<DeviceDto>> GetDevicesAsync(string filter, int page, int pageSize, string? projectId = null, List<string>? allowedProjectIds = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Suspends a device preventing it from receiving further OTA updates until reactivated.
        /// </summary>
        /// <param name="deviceId">The identifier of the device to suspend.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SuspendDeviceAsync(string deviceId, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Decommissions a device permanently removing it from active OTA operations while preserving history.
        /// </summary>
        /// <param name="deviceId">The identifier of the device to decommission.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DecommissionDeviceAsync(string deviceId, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Registers multiple devices in a single call. Processes each row independently;
        /// failures are collected and returned without aborting the remaining rows.
        /// </summary>
        Task<BulkRegisterResult> BulkRegisterDevicesAsync(BulkRegisterDeviceRequest request, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reactivates a previously suspended device, making it eligible for OTA updates again.
        /// </summary>
        /// <param name="deviceId">The identifier of the device to activate.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ActivateDeviceAsync(string deviceId, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the list of approved firmware versions compatible with a device's model.
        /// </summary>
        Task<List<AvailableFirmwareDto>> GetAvailableFirmwareAsync(string deviceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a direct OTA job targeting a single device with a specified firmware version.
        /// </summary>
        Task<string> PushFirmwareToDeviceAsync(string deviceId, string firmwareVersionId, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the paginated OTA update history for a device.
        /// </summary>
        /// <param name="deviceId">The device identifier (MongoDB ObjectId).</param>
        /// <param name="page">One-based page number.</param>
        /// <param name="pageSize">Number of results per page.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Paged result of OTA history items.</returns>
        Task<PagedResult<DeviceOtaHistoryItemDto>> GetDeviceOtaHistoryAsync(string deviceId, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns all OTA-ready devices (Active status) without requiring authentication.
        /// Intended for public/device-facing consumption.
        /// </summary>
        Task<List<OtaReadyDeviceDto>> GetOtaReadyDevicesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Recomputes the denormalised <c>HasActiveOtaJob</c> flag on every device by
        /// inspecting the ota_jobs collection. Returns the number of devices updated.
        /// SuperAdmin only.
        /// </summary>
        Task<long> BackfillActiveOtaJobFlagAsync(CancellationToken cancellationToken = default);
    }
}
