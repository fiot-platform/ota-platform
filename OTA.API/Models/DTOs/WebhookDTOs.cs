using System.ComponentModel.DataAnnotations;

namespace OTA.API.Models.DTOs
{
    // ─────────────────────────────────────────────────────────────────────────
    // Webhook Event DTOs
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Summary row for paginated webhook event list responses.</summary>
    public sealed class WebhookEventListDto
    {
        /// <summary>Platform event identifier.</summary>
        public string EventId { get; set; } = string.Empty;

        /// <summary>Gitea internal repository ID the event originated from.</summary>
        public long GiteaRepoId { get; set; }

        /// <summary>Type of Gitea event (Push, Release, Tag, Create, Delete).</summary>
        public string GiteaEventType { get; set; } = string.Empty;

        /// <summary>Gitea delivery ID (X-Gitea-Delivery header); used for deduplication.</summary>
        public string DeliveryId { get; set; } = string.Empty;

        /// <summary>Current processing status of the event.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Number of times processing has been retried after failure.</summary>
        public int RetryCount { get; set; }

        /// <summary>UTC timestamp when the webhook was received.</summary>
        public DateTime ReceivedAt { get; set; }

        /// <summary>UTC timestamp when processing completed; null if not yet processed.</summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>Most recent processing error message; null if no error.</summary>
        public string? ProcessingError { get; set; }
    }

    /// <summary>Full webhook event detail returned by GET /api/webhook-events/{eventId}.</summary>
    public sealed class WebhookEventDetailDto
    {
        /// <summary>Platform event identifier.</summary>
        public string EventId { get; set; } = string.Empty;

        /// <summary>Gitea internal repository ID.</summary>
        public long GiteaRepoId { get; set; }

        /// <summary>Type of Gitea event as a string.</summary>
        public string GiteaEventType { get; set; } = string.Empty;

        /// <summary>
        /// Full raw JSON payload received from Gitea.
        /// Stored verbatim for debugging and replay purposes.
        /// </summary>
        public string RawPayload { get; set; } = string.Empty;

        /// <summary>Gitea delivery ID.</summary>
        public string DeliveryId { get; set; } = string.Empty;

        /// <summary>Current processing status.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Most recent processing error description.</summary>
        public string? ProcessingError { get; set; }

        /// <summary>Number of processing retry attempts.</summary>
        public int RetryCount { get; set; }

        /// <summary>UTC timestamp when the webhook was received by the API.</summary>
        public DateTime ReceivedAt { get; set; }

        /// <summary>UTC timestamp when processing successfully completed.</summary>
        public DateTime? ProcessedAt { get; set; }

        /// <summary>UTC timestamp when this event record was created in MongoDB.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Identifier of the platform entity created or updated as a result of processing this event.
        /// For example, a FirmwareId when a Release event creates a new firmware version.
        /// </summary>
        public string? ResultingEntityId { get; set; }

        /// <summary>Type of the resulting entity (e.g., "FirmwareVersion").</summary>
        public string? ResultingEntityType { get; set; }
    }

    /// <summary>Request body for manually triggering a retry of a failed or stuck webhook event.</summary>
    public sealed class RetryWebhookRequest
    {
        /// <summary>
        /// Optional override for the maximum retry count check.
        /// If true, the event will be retried even if it has reached the normal retry limit.
        /// Only SuperAdmin and PlatformAdmin may set this to true.
        /// </summary>
        public bool ForceRetry { get; set; } = false;

        /// <summary>Optional reason for the manual retry (recorded in the audit log).</summary>
        [MaxLength(500)]
        public string? Reason { get; set; }
    }

    /// <summary>Filter parameters for the webhook event list endpoint.</summary>
    public sealed class WebhookEventFilterRequest
    {
        /// <summary>Filter by Gitea repository ID.</summary>
        public long? GiteaRepoId { get; set; }

        /// <summary>Filter by event type (e.g., "Release").</summary>
        [MaxLength(50)]
        public string? GiteaEventType { get; set; }

        /// <summary>Filter by processing status.</summary>
        [MaxLength(50)]
        public string? Status { get; set; }

        /// <summary>Inclusive start of received-at timestamp range.</summary>
        public DateTime? DateFrom { get; set; }

        /// <summary>Inclusive end of received-at timestamp range.</summary>
        public DateTime? DateTo { get; set; }

        /// <summary>Page number (1-based).</summary>
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        /// <summary>Records per page (max 200).</summary>
        [Range(1, 200)]
        public int PageSize { get; set; } = 25;
    }

    /// <summary>Wrapper for paginated webhook event list responses.</summary>
    public sealed class PagedWebhookEventListResponse
    {
        public List<WebhookEventListDto> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public long TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
