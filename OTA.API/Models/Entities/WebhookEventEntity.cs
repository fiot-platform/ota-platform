using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using OTA.API.Models.Enums;

namespace OTA.API.Models.Entities
{
    /// <summary>
    /// MongoDB document storing incoming Gitea webhook events for durable processing.
    /// Collection: webhookEvents
    /// Indexes:
    ///   - Unique: DeliveryId
    ///   - Single: Status
    ///   - Compound: {Status, RetryCount} for retry queries
    ///   - Single: ReceivedAt (descending)
    /// </summary>
    public sealed class WebhookEventEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("deliveryId")]
        public string DeliveryId { get; set; } = string.Empty;

        [BsonElement("eventType")]
        public string EventType { get; set; } = string.Empty;

        [BsonElement("repositoryId")]
        [BsonIgnoreIfNull]
        public string? RepositoryId { get; set; }

        [BsonElement("giteaRepoId")]
        public long GiteaRepoId { get; set; }

        [BsonElement("payload")]
        public string Payload { get; set; } = string.Empty;

        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public WebhookEventStatus Status { get; set; } = WebhookEventStatus.Received;

        [BsonElement("retryCount")]
        public int RetryCount { get; set; } = 0;

        [BsonElement("maxRetries")]
        public int MaxRetries { get; set; } = 3;

        [BsonElement("lastError")]
        [BsonIgnoreIfNull]
        public string? LastError { get; set; }

        [BsonElement("processedAt")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? ProcessedAt { get; set; }

        [BsonElement("nextRetryAt")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? NextRetryAt { get; set; }

        [BsonElement("receivedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
