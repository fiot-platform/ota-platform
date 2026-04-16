using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OTA.API.Models.Entities
{
    /// <summary>
    /// Stores a single OTA lifecycle event reported by a device over MQTT.
    /// One document is written per terminal/start status packet
    /// (start | success | failed | rollback).  "inprogress" packets are not
    /// stored here — they only update the live progress fields on DeviceEntity.
    /// Collection: DeviceOtaEvents
    /// </summary>
    public sealed class DeviceOtaEventEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        /// <summary>MongoDB ObjectId of the device (_id in the Devices collection).</summary>
        [BsonElement("deviceId")]
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>Serial number / MAC / IMEI of the device (for display without a join).</summary>
        [BsonElement("serialNumber")]
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>OTA lifecycle status: start | success | failed | rollback.</summary>
        [BsonElement("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>Progress percentage at the time of this event (0–100).</summary>
        [BsonElement("progress")]
        public int Progress { get; set; }

        /// <summary>Firmware version involved in this update attempt.</summary>
        [BsonElement("version")]
        [BsonIgnoreIfNull]
        public string? Version { get; set; }

        /// <summary>Error code reported by the device (0 = no error).</summary>
        [BsonElement("errorCode")]
        public int ErrorCode { get; set; }

        /// <summary>UTC timestamp when this event was recorded by the server.</summary>
        [BsonElement("timestamp")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
