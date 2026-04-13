using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OTA.API.Models.DTOs;

namespace OTA.API.Services.Interfaces
{
    /// <summary>
    /// Service interface for processing incoming Gitea webhook events.
    /// </summary>
    public interface IGiteaWebhookService
    {
        /// <summary>
        /// Processes an incoming webhook payload from Gitea.
        /// Saves the raw event to the repository event store, determines the event type, and dispatches to the
        /// appropriate handler (e.g. on release event -> firmware sync; on push event -> metadata log).
        /// </summary>
        /// <param name="payload">The raw JSON webhook payload body from Gitea.</param>
        /// <param name="eventType">The X-Gitea-Event header value identifying the event type.</param>
        /// <param name="deliveryId">The X-Gitea-Delivery header value uniquely identifying the delivery (used for deduplication).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the event was processed successfully; false if processing was skipped or deferred.</returns>
        Task<bool> ProcessWebhookAsync(string payload, string eventType, string deliveryId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reprocesses a previously failed webhook event, resetting its retry counter and re-running the handler.
        /// </summary>
        /// <param name="eventId">The internal identifier of the <see cref="OTA.API.Models.Entities.RepositoryEventEntity"/> to reprocess.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ReprocessEventAsync(string eventId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves recent webhook events with optional filtering for the event dashboard.
        /// </summary>
        /// <param name="filter">Optional filter parameters such as repository, status, or date range.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of recent repository event DTOs.</returns>
        Task<List<RepositoryEventDto>> GetRecentEventsAsync(RepositoryEventFilterRequest? filter, CancellationToken cancellationToken = default);
    }
}
