namespace OTA.API.Services.Interfaces
{
    /// <summary>
    /// Abstraction over the MQTT client used by the OTA server to publish firmware
    /// metadata to devices and to broadcast user-acknowledgement packets.
    /// </summary>
    public interface IMqttService
    {
        /// <summary>Returns true when the underlying MQTT client is currently connected.</summary>
        bool IsConnected { get; }

        /// <summary>
        /// Publishes a UTF-8 JSON payload to the specified MQTT topic with QoS 1.
        /// </summary>
        /// <param name="topic">Full topic string.</param>
        /// <param name="payload">JSON string to publish.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task PublishAsync(string topic, string payload, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes the OTA firmware metadata response to a specific device.
        /// Topic: OTA/{deviceId}/Card
        /// </summary>
        /// <param name="deviceId">Device serial number / identifier.</param>
        /// <param name="payload">Serialised JSON of the OTA update envelope.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task PublishOtaMetadataAsync(string deviceId, string payload, CancellationToken cancellationToken = default);
    }
}
