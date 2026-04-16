using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OTA.API.Models.Entities
{
    /// <summary>
    /// Persisted record of a platform notification broadcast to one or more user roles.
    /// Each user's read status is tracked via <see cref="ReadByUserIds"/>.
    /// </summary>
    public sealed class NotificationLogEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("body")]
        public string Body { get; set; } = string.Empty;

        [BsonElement("data")]
        [BsonIgnoreIfNull]
        public Dictionary<string, string>? Data { get; set; }

        /// <summary>Role names that should see this notification (e.g. "SuperAdmin", "QA").</summary>
        [BsonElement("targetRoles")]
        public List<string> TargetRoles { get; set; } = new();

        /// <summary>User IDs (GUID strings) who have already read this notification.</summary>
        [BsonElement("readByUserIds")]
        public List<string> ReadByUserIds { get; set; } = new();

        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
