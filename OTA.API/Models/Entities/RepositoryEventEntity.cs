using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using OTA.API.Models.Enums;

namespace OTA.API.Models.Entities
{
    /// <summary>
    /// MongoDB document storing an inbound Gitea webhook event payload with its full processing lifecycle.
    /// The outbox pattern is applied: the event is persisted immediately on receipt and processed
    /// asynchronously by a background service, enabling reliable at-least-once processing with retries.
    /// Collection: repository_events
    /// Indexes:
    ///   - Unique: EventId
    ///   - Unique: DeliveryId
    ///   - Single: GiteaRepoId
    ///   - Single: Status
    ///   - Compound: {Status, RetryCount} (for retry worker queries)
    ///   - Single: ReceivedAt (descending, for dashboard and audit views)
    /// </summary>
    public sealed class RepositoryEventEntity
    {
        /// <summary>MongoDB internal ObjectId (_id).</summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Platform-generated unique identifier for this event record (GUID string).</summary>
        [BsonElement("eventId")]
        public string EventId { get; set; } = string.Empty;

        /// <summary>
        /// Gitea's internal repository ID extracted from the webhook payload.
        /// Used to correlate the event with a registered RepositoryEntity.
        /// </summary>
        [BsonElement("giteaRepoId")]
        public long GiteaRepoId { get; set; }

        /// <summary>
        /// The type of Gitea event (Push, Release, Tag, Create, Delete) as parsed
        /// from the X-Gitea-Event HTTP header.
        /// </summary>
        [BsonElement("giteaEventType")]
        [BsonRepresentation(BsonType.String)]
        public GiteaEventType GiteaEventType { get; set; }

        /// <summary>
        /// The full raw JSON body of the Gitea webhook payload, stored verbatim for
        /// auditability, replay, and debugging. May be large for push events with many commits.
        /// </summary>
        [BsonElement("rawPayload")]
        public string RawPayload { get; set; } = string.Empty;

        /// <summary>
        /// The value of the X-Gitea-Delivery header — a unique delivery attempt identifier
        /// assigned by Gitea. Used to deduplicate redelivered webhook events.
        /// </summary>
        [BsonElement("deliveryId")]
        public string DeliveryId { get; set; } = string.Empty;

        /// <summary>Current processing status of this webhook event.</summary>
        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public WebhookEventStatus Status { get; set; } = WebhookEventStatus.Received;

        /// <summary>
        /// Error message from the most recent failed processing attempt.
        /// Null when the event has been processed successfully.
        /// </summary>
        [BsonElement("processingError")]
        [BsonIgnoreIfNull]
        public string? ProcessingError { get; set; }

        /// <summary>
        /// Number of times the background processor has retried this event after failure.
        /// Processing is abandoned when RetryCount reaches the configured maximum.
        /// </summary>
        [BsonElement("retryCount")]
        public int RetryCount { get; set; } = 0;

        /// <summary>UTC timestamp when the webhook payload was received by the API.</summary>
        [BsonElement("receivedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        /// <summary>UTC timestamp when processing of this event was successfully completed.</summary>
        [BsonElement("processedAt")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? ProcessedAt { get; set; }

        /// <summary>UTC timestamp when this event record was created in MongoDB.</summary>
        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
