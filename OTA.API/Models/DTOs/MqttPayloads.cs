using System.Text.Json.Serialization;

namespace OTA.API.Models.DTOs
{
    // ─────────────────────────────────────────────────────────────────────────
    // MQTT Topic Constants  (FSI Document §4)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Centralised MQTT topic strings matching the OTA FSI document specification.
    /// </summary>
    public static class MqttTopics
    {
        /// <summary>
        /// Device → Server.  Device publishes an OTA request here.
        /// Topic: OTA/Card2Web/Server
        /// </summary>
        public const string OtaRequestSubscribe = "OTA/Card2Web/Server";

        /// <summary>
        /// Device → Server.  Device publishes progress/success/failure status here.
        /// Wildcard: OTA/+/Status  (+ matches the device serial number)
        /// </summary>
        public const string OtaStatusSubscribeFilter = "OTA/+/Status";

        /// <summary>
        /// Server → Device.  Server publishes OTA metadata to a specific device.
        /// Template: OTA/{deviceId}/Card
        /// </summary>
        public static string OtaMetadataPublish(string deviceId) => $"OTA/{deviceId}/Card";

        /// <summary>
        /// Server → Device.  User-acknowledgement packet forwarded to device.
        /// Template: topic/{deviceId}/App2FIoT
        /// </summary>
        public static string UserAckPublish(string deviceId) => $"topic/{deviceId}/App2FIoT";

        /// <summary>
        /// Server → Broker.  Published when a device is registered on the platform.
        /// Template: OTA/{deviceId}/Status
        /// </summary>
        public static string DeviceRegisteredPublish(string deviceId) => $"OTA/{deviceId}/Status";

        /// <summary>Extracts the device identifier segment from an OTA status topic (OTA/{id}/Status).</summary>
        public static string? ExtractDeviceIdFromStatusTopic(string topic)
        {
            // topic format: OTA/{deviceId}/Status
            var parts = topic.Split('/');
            return parts.Length == 3 && parts[0] == "OTA" && parts[2] == "Status"
                ? parts[1]
                : null;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Device → Server  :  OTA Request  (FSI Document §5 Step 1)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Outer envelope published by the device on OTA/Card2Web/Server.</summary>
    public sealed class MqttOtaRequestEnvelope
    {
        [JsonPropertyName("otaRequest")]
        public MqttOtaRequest? OtaRequest { get; set; }
    }

    /// <summary>OTA request payload sent by the device.</summary>
    public sealed class MqttOtaRequest
    {
        /// <summary>Device identifier — maps to SerialNumber / MacImeiIp in the platform.</summary>
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>Firmware version currently running on the device.</summary>
        [JsonPropertyName("currentVersion")]
        public string CurrentVersion { get; set; } = string.Empty;

        /// <summary>ISO-8601 timestamp from the device clock.</summary>
        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }

        /// <summary>Device IP address (informational).</summary>
        [JsonPropertyName("ipAddress")]
        public string? IpAddress { get; set; }

        /// <summary>
        /// What triggered this OTA request.
        /// Values: "power on" | "device reset" | "user acknowledgement"
        /// </summary>
        [JsonPropertyName("trigger")]
        public string? Trigger { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Server → Device  :  OTA Metadata Response  (FSI Document §6)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Outer envelope published by the server on topic/{deviceId}/App2FIoT.</summary>
    public sealed class MqttOtaUpdateEnvelope
    {
        [JsonPropertyName("otaUpdate")]
        public MqttOtaUpdate OtaUpdate { get; set; } = new();
    }

    /// <summary>OTA metadata published by the server to the device.</summary>
    public sealed class MqttOtaUpdate
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; } = string.Empty;

        [JsonPropertyName("files")]
        public List<MqttFirmwareFile> Files { get; set; } = new();

        [JsonPropertyName("releaseDate")]
        public string? ReleaseDate { get; set; }

        [JsonPropertyName("mandatory")]
        public bool Mandatory { get; set; }

        [JsonPropertyName("rollbackSupported")]
        public bool RollbackSupported { get; set; } = true;
    }

    /// <summary>A single firmware file entry inside the OTA metadata.</summary>
    public sealed class MqttFirmwareFile
    {
        [JsonPropertyName("fileIndex")]
        public int FileIndex { get; set; } = 1;

        [JsonPropertyName("downloadUrl")]
        public string DownloadUrl { get; set; } = string.Empty;

        [JsonPropertyName("fileSize")]
        public long FileSize { get; set; }

        [JsonPropertyName("checksum")]
        public MqttChecksum Checksum { get; set; } = new();
    }

    /// <summary>Checksum (integrity) information for a firmware file.</summary>
    public sealed class MqttChecksum
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "SHA256";

        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Server → Device  :  No Update Available
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Published on topic/{deviceId}/App2FIoT when no update is available.</summary>
    public sealed class MqttNoUpdateEnvelope
    {
        [JsonPropertyName("otaUpdate")]
        public MqttNoUpdate OtaUpdate { get; set; } = new();
    }

    /// <summary>Minimal response indicating the device is up to date.</summary>
    public sealed class MqttNoUpdate
    {
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = "up-to-date";

        [JsonPropertyName("currentVersion")]
        public string CurrentVersion { get; set; } = string.Empty;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Device → Server  :  OTA Status Updates  (FSI Document §8, §11–13, §16)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Outer envelope published by the device on OTA/{deviceId}/Status.</summary>
    public sealed class MqttOtaStatusEnvelope
    {
        [JsonPropertyName("otaStatus")]
        public MqttOtaStatus? OtaStatus { get; set; }
    }

    /// <summary>OTA status update payload sent by the device.</summary>
    public sealed class MqttOtaStatus
    {
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; } = string.Empty;

        [JsonPropertyName("currentVersion")]
        public string CurrentVersion { get; set; } = string.Empty;

        /// <summary>
        /// OTA lifecycle status.
        /// Values: "start" | "inprogress" | "success" | "failed" | "rollback"
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>Download/install progress 0–100. 100 = complete.</summary>
        [JsonPropertyName("progress")]
        public int Progress { get; set; }

        /// <summary>Error code (0 = no error). See FSI §14 for code definitions.</summary>
        [JsonPropertyName("errorCode")]
        public int ErrorCode { get; set; }

        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Server → Broker  :  Device Registration Event
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Published by the server on OTA/{deviceId}/Registered when a device is
    /// successfully registered on the OTA platform. The device can subscribe to
    /// this topic to receive its platform-assigned identifiers.
    /// </summary>
    public sealed class MqttDeviceRegisteredPayload
    {
        [JsonPropertyName("event")]
        public string Event { get; set; } = "device_registered";

        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; } = string.Empty;

        [JsonPropertyName("serialNumber")]
        public string SerialNumber { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("projectName")]
        public string ProjectName { get; set; } = string.Empty;

        [JsonPropertyName("currentFirmwareVersion")]
        public string CurrentFirmwareVersion { get; set; } = string.Empty;

        [JsonPropertyName("registeredAt")]
        public string RegisteredAt { get; set; } = string.Empty;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Server → Broker  :  OTA Process Device Status Event
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Published by the server on the device's configured publishTopic whenever
    /// an OTA status packet is received (start | inprogress | success | failed | rollback).
    /// </summary>
    public sealed class MqttOtaProcessPayload
    {
        [JsonPropertyName("event")]
        public string Event { get; set; } = "ota_status";

        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; } = string.Empty;

        [JsonPropertyName("serialNumber")]
        public string SerialNumber { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("projectName")]
        public string ProjectName { get; set; } = string.Empty;

        [JsonPropertyName("otaStatus")]
        public string OtaStatus { get; set; } = string.Empty;

        [JsonPropertyName("otaProgress")]
        public int OtaProgress { get; set; }

        [JsonPropertyName("currentFirmwareVersion")]
        public string CurrentFirmwareVersion { get; set; } = string.Empty;

        [JsonPropertyName("otaTargetVersion")]
        public string? OtaTargetVersion { get; set; }

        [JsonPropertyName("errorCode")]
        public int ErrorCode { get; set; }

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = string.Empty;
    }
}
