using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using OTA.API.Models.Enums;

namespace OTA.API.Models.Entities
{
    /// <summary>
    /// MongoDB document representing a registered IoT device in the OTA platform.
    /// Collection: devices
    /// Indexes:
    ///   - Unique: DeviceId
    ///   - Unique: SerialNumber
    ///   - Single: CustomerId
    ///   - Single: SiteId
    ///   - Single: Status
    ///   - Compound: {CustomerId, Status}
    ///   - Compound: {Model, HardwareRevision}
    /// </summary>
    public sealed class DeviceEntity
    {
        /// <summary>MongoDB internal ObjectId (_id).</summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Platform-generated unique identifier for the device (GUID string).
        /// Embedded in the device's JWT token as the sub/deviceId claim.
        /// </summary>
        [BsonElement("deviceId")]
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// Manufacturer serial number; must be globally unique within the platform.
        /// Used by the device during registration to prove its identity.
        /// </summary>
        [BsonElement("serialNumber")]
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// Device model identifier (e.g., "EDGE-GW-V2").
        /// Matched against FirmwareVersionEntity.SupportedModels during check-update evaluation.
        /// </summary>
        [BsonElement("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Hardware revision string (e.g., "REV-B").
        /// Matched against FirmwareVersionEntity.SupportedHardwareRevisions during check-update evaluation.
        /// </summary>
        [BsonElement("hardwareRevision")]
        [BsonIgnoreIfNull]
        public string? HardwareRevision { get; set; }

        /// <summary>Customer tenant identifier that owns this device.</summary>
        [BsonElement("customerId")]
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>Display name of the customer (denormalised for read performance).</summary>
        [BsonElement("customerName")]
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        /// Site or location identifier where the device is physically deployed.
        /// Used for Site-targeted rollouts.
        /// </summary>
        [BsonElement("siteId")]
        [BsonIgnoreIfNull]
        public string? SiteId { get; set; }

        /// <summary>Display name of the site (denormalised).</summary>
        [BsonElement("siteName")]
        [BsonIgnoreIfNull]
        public string? SiteName { get; set; }

        /// <summary>The firmware version string currently running on the device.</summary>
        [BsonElement("currentFirmwareVersion")]
        [BsonIgnoreIfNull]
        public string? CurrentFirmwareVersion { get; set; }

        /// <summary>The firmware version string that was running before the most recent update.</summary>
        [BsonElement("previousFirmwareVersion")]
        [BsonIgnoreIfNull]
        public string? PreviousFirmwareVersion { get; set; }

        /// <summary>
        /// MAC address, IMEI, or IP address used as the primary device identifier.
        /// Stored in SerialNumber for uniqueness enforcement; also kept here for display.
        /// </summary>
        [BsonElement("macImeiIp")]
        [BsonIgnoreIfNull]
        public string? MacImeiIp { get; set; }

        /// <summary>Identifier of the project this device belongs to.</summary>
        [BsonElement("projectId")]
        [BsonIgnoreIfNull]
        public string? ProjectId { get; set; }

        /// <summary>Name of the project this device belongs to.</summary>
        [BsonElement("projectName")]
        [BsonIgnoreIfNull]
        public string? ProjectName { get; set; }

        /// <summary>Current operational status of the device.</summary>
        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public DeviceStatus Status { get; set; } = DeviceStatus.Active;

        /// <summary>UTC timestamp of the device's most recent heartbeat / check-update call.</summary>
        [BsonElement("lastHeartbeatAt")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? LastHeartbeatAt { get; set; }

        /// <summary>UTC timestamp when the device was first registered on the platform.</summary>
        [BsonElement("registeredAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        /// <summary>UTC timestamp of the most recent update to this device record.</summary>
        [BsonElement("updatedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // ── OTA Progress (updated live via MQTT status packets) ───────────────

        /// <summary>
        /// Last OTA lifecycle status reported by the device via MQTT.
        /// Values: "start" | "inprogress" | "success" | "failed" | "rollback"
        /// Null when no OTA has been attempted or progress has been cleared.
        /// </summary>
        [BsonElement("otaStatus")]
        [BsonIgnoreIfNull]
        public string? OtaStatus { get; set; }

        /// <summary>Download/install progress 0–100 reported by the device.</summary>
        [BsonElement("otaProgress")]
        [BsonIgnoreIfDefault]
        public int OtaProgress { get; set; }

        /// <summary>The firmware version the device is currently updating to.</summary>
        [BsonElement("otaTargetVersion")]
        [BsonIgnoreIfNull]
        public string? OtaTargetVersion { get; set; }

        /// <summary>UTC timestamp of the last OTA status packet received.</summary>
        [BsonElement("otaUpdatedAt")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? OtaUpdatedAt { get; set; }

        /// <summary>UserId of the platform user who registered this device.</summary>
        [BsonElement("registeredByUserId")]
        [BsonIgnoreIfNull]
        public string? RegisteredByUserId { get; set; }

        /// <summary>
        /// MQTT topic used to publish registration / update events for this device.
        /// Defaults to OTA/{SerialNumber}/Status when not explicitly set.
        /// </summary>
        [BsonElement("publishTopic")]
        [BsonIgnoreIfNull]
        public string? PublishTopic { get; set; }

        /// <summary>
        /// Free-form tags for grouping and filtering (e.g., ["warehouse-zone-a", "high-priority"]).
        /// </summary>
        [BsonElement("tags")]
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// Arbitrary key-value metadata reported by the device (e.g., OS version, chip ID, uptime).
        /// Updated on each heartbeat / check-update call.
        /// </summary>
        [BsonElement("metadata")]
        public Dictionary<string, string> Metadata { get; set; } = new();

        // ── Alias properties for service layer compatibility ───────────────────
        [BsonIgnore] public FirmwareChannel FirmwareChannel { get; set; } = FirmwareChannel.Production;
        [BsonIgnore] public DateTime LastSeen { get => LastHeartbeatAt ?? RegisteredAt; set => LastHeartbeatAt = value; }
        [BsonIgnore] public string? CreatedByUserId { get => RegisteredByUserId; set => RegisteredByUserId = value; }
        [BsonIgnore] public DateTime CreatedAt { get => RegisteredAt; set => RegisteredAt = value; }
    }
}
