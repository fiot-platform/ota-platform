using OTA.API.Models.Entities;

namespace OTA.API.Repositories.Interfaces
{
    /// <summary>
    /// Repository for MQTT-sourced OTA lifecycle events per device.
    /// </summary>
    public interface IDeviceOtaEventRepository
    {
        /// <summary>Appends a new OTA event document.</summary>
        Task LogAsync(DeviceOtaEventEntity evt, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns all events for a device, newest first.
        /// </summary>
        Task<List<DeviceOtaEventEntity>> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default);
    }
}
