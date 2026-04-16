using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OTA.API.Models.Entities;
using OTA.API.Models.Enums;

namespace OTA.API.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for DeviceEntity providing device-specific query and state-update operations.
    /// </summary>
    public interface IDeviceRepository : IBaseRepository<DeviceEntity>
    {
        /// <summary>
        /// Retrieves a device by its unique serial number.
        /// </summary>
        /// <param name="serialNumber">The device's hardware serial number.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The matching device entity, or null if not found.</returns>
        Task<DeviceEntity?> GetBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all devices belonging to the specified customer.
        /// </summary>
        /// <param name="customerId">The customer identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of devices for the customer.</returns>
        Task<List<DeviceEntity>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all devices located at the specified site.
        /// </summary>
        /// <param name="siteId">The site identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of devices at the site.</returns>
        Task<List<DeviceEntity>> GetBySiteIdAsync(string siteId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all devices with the specified operational status.
        /// </summary>
        /// <param name="status">The device status to filter by.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of devices with the specified status.</returns>
        Task<List<DeviceEntity>> GetByStatusAsync(DeviceStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches devices by serial number, model, or location with pagination.
        /// </summary>
        /// <param name="filter">Search text to match against device serial number, model, or site.</param>
        /// <param name="page">One-based page number.</param>
        /// <param name="pageSize">Number of results per page.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Paged list of matching device entities.</returns>
        Task<List<DeviceEntity>> SearchAsync(string filter, int page, int pageSize, string? projectId = null, List<string>? allowedProjectIds = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Counts devices matching the given filter text.
        /// </summary>
        /// <param name="filter">Search text to match against device fields.</param>
        /// <param name="projectId">Optional explicit project identifier to scope results.</param>
        /// <param name="allowedProjectIds">When non-null, restricts results to the given project IDs (role scope).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Total number of matching devices.</returns>
        Task<long> CountAsync(string filter, string? projectId = null, List<string>? allowedProjectIds = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the last-seen heartbeat timestamp for the specified device.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="lastSeen">The UTC timestamp of the most recent heartbeat.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UpdateHeartbeatAsync(string deviceId, DateTime lastSeen, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the current firmware version reported by the device.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="firmwareVersion">The new firmware version string reported by the device.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UpdateFirmwareVersionAsync(string deviceId, string firmwareVersion, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the live OTA progress fields on a device.
        /// Called on every MQTT OTA status packet from the device.
        /// </summary>
        /// <param name="deviceId">The MongoDB ObjectId of the device.</param>
        /// <param name="otaStatus">Status string (start | inprogress | success | failed | rollback).</param>
        /// <param name="otaProgress">Progress percentage 0–100.</param>
        /// <param name="otaTargetVersion">Firmware version being installed.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UpdateOtaProgressAsync(string deviceId, string otaStatus, int otaProgress, string? otaTargetVersion, CancellationToken cancellationToken = default);
    }
}
