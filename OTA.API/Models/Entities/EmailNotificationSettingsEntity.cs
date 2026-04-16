using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OTA.API.Models.Entities
{
    public sealed class EmailNotificationSettingsEntity
    {
        [BsonId, BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        // Firmware events
        public bool OnFirmwareSubmitted  { get; set; } = true;
        public bool OnFirmwareApproved   { get; set; } = true;
        public bool OnFirmwareRejected   { get; set; } = true;
        public bool OnFirmwareQAVerified { get; set; } = false;

        // Rollout events
        public bool OnRolloutStarted   { get; set; } = false;
        public bool OnRolloutCompleted { get; set; } = true;
        public bool OnRolloutFailed    { get; set; } = true;

        // Device events
        public bool OnDeviceOtaFailed   { get; set; } = true;
        public bool OnDeviceRegistered  { get; set; } = false;

        // User events
        public bool OnNewUserCreated  { get; set; } = true;
        public bool OnUserDeactivated { get; set; } = false;

        // Recipients
        public List<string> NotifyEmails { get; set; } = new(); // additional CCs

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string UpdatedBy { get; set; } = string.Empty;
    }
}
