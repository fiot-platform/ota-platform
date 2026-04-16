using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OTA.API.Models.Entities;
using OTA.API.Models.Enums;

namespace OTA.API.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for RolloutEntity providing rollout-specific query and status-update operations.
    /// </summary>
    public interface IRolloutRepository : IBaseRepository<RolloutEntity>
    {
        /// <summary>
        /// Retrieves all rollouts associated with the specified project.
        /// </summary>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of rollout entities for the project.</returns>
        Task<List<RolloutEntity>> GetByProjectIdAsync(string projectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all rollouts with the specified status.
        /// </summary>
        /// <param name="status">The rollout status to filter by.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of rollout entities with the specified status.</returns>
        Task<List<RolloutEntity>> GetByStatusAsync(RolloutStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all rollouts using the specified firmware version.
        /// </summary>
        /// <param name="firmwareId">The firmware entity identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of rollout entities referencing the firmware.</returns>
        Task<List<RolloutEntity>> GetByFirmwareIdAsync(string firmwareId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches rollouts by name or description with pagination.
        /// </summary>
        /// <param name="filter">Search text to match against rollout name or description.</param>
        /// <param name="page">One-based page number.</param>
        /// <param name="pageSize">Number of results per page.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Paged list of matching rollout entities.</returns>
        Task<List<RolloutEntity>> SearchAsync(string filter, int page, int pageSize, string? projectId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the status of the specified rollout.
        /// </summary>
        /// <param name="rolloutId">The rollout identifier.</param>
        /// <param name="status">The new rollout status.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UpdateStatusAsync(string rolloutId, RolloutStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Atomically increments the rollout's job counters (total, success, failure, pending).
        /// </summary>
        /// <param name="rolloutId">The rollout identifier.</param>
        /// <param name="successDelta">Change to apply to SuccessCount.</param>
        /// <param name="failureDelta">Change to apply to FailureCount.</param>
        /// <param name="pendingDelta">Change to apply to PendingCount.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UpdateCountersAsync(string rolloutId, int successDelta, int failureDelta, int pendingDelta, CancellationToken cancellationToken = default);
    }
}
