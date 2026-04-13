using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OTA.API.Models.Entities;
using OTA.API.Models.Enums;

namespace OTA.API.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for RepositoryEventEntity providing webhook event query operations.
    /// </summary>
    public interface IRepositoryEventRepository : IBaseRepository<RepositoryEventEntity>
    {
        /// <summary>
        /// Retrieves all webhook events received for the specified Gitea repository.
        /// </summary>
        /// <param name="giteaRepoId">The Gitea repository identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of repository event entities for the repository.</returns>
        Task<List<RepositoryEventEntity>> GetByGiteaRepoIdAsync(string giteaRepoId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all webhook events with the specified processing status.
        /// </summary>
        /// <param name="status">The webhook event processing status to filter by.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of events with the specified status.</returns>
        Task<List<RepositoryEventEntity>> GetByStatusAsync(WebhookEventStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves events that failed processing and are eligible for retry.
        /// </summary>
        /// <param name="maxRetries">Maximum retry attempts already made (events below this threshold are returned).</param>
        /// <param name="limit">Maximum number of failed events to return.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of failed event entities eligible for reprocessing.</returns>
        Task<List<RepositoryEventEntity>> GetFailedEventsAsync(int maxRetries = 3, int limit = 50, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the most recent webhook events ordered by received-at timestamp descending.
        /// </summary>
        /// <param name="limit">Maximum number of recent events to return.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of the most recent event entities.</returns>
        Task<List<RepositoryEventEntity>> GetRecentEventsAsync(int limit = 20, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a count of webhook events grouped by processing status.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Dictionary mapping WebhookEventStatus to count.</returns>
        Task<Dictionary<WebhookEventStatus, long>> CountByStatusAsync(CancellationToken cancellationToken = default);
    }
}
