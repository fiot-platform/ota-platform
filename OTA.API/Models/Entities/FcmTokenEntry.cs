using MongoDB.Bson.Serialization.Attributes;

namespace OTA.API.Models.Entities
{
    /// <summary>
    /// Embedded subdocument stored inside <see cref="UserEntity.FcmTokens"/>.
    /// Represents a single Firebase Cloud Messaging (FCM) registration token for a
    /// user's device or browser.
    /// </summary>
    public sealed class FcmTokenEntry
    {
        /// <summary>The FCM registration token string obtained from the Firebase SDK on the client.</summary>
        [BsonElement("token")]
        public string Token { get; set; } = string.Empty;

        /// <summary>Optional human-readable label supplied by the client (e.g., "Chrome on MacBook").</summary>
        [BsonElement("deviceLabel")]
        [BsonIgnoreIfNull]
        public string? DeviceLabel { get; set; }

        /// <summary>UTC timestamp when this token was first registered or last refreshed.</summary>
        [BsonElement("registeredAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    }
}
