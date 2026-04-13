using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OTA.API.Models.Entities;
using OTA.API.Models.Enums;

namespace OTA.API.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for OtaJobEntity providing OTA job query and bulk-write operations.
    /// </summary>
    public interface IOtaJobRepository : IBaseRepository<OtaJobEntity>
    {
        /// <summary>
        /// Retrieves all OTA jobs associated with the specified rollout.
        /// </summary>
        /// <param name="rolloutId">The rollout identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of OTA job entities for the rollout.</returns>
        Task<List<OtaJobEntity>> GetByRolloutIdAsync(string rolloutId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all OTA jobs targeting the specified device.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of OTA job entities for the device.</returns>
        Task<List<OtaJobEntity>> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all OTA jobs with the specified status.
        /// </summary>
        /// <param name="status">The OTA job status to filter by.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of OTA job entities with the specified status.</returns>
        Task<List<OtaJobEntity>> GetByStatusAsync(OtaJobStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all pending (queued) OTA jobs ready for dispatch, ordered by creation time.
        /// </summary>
        /// <param name="limit">Maximum number of pending jobs to return.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of pending OTA job entities.</returns>
        Task<List<OtaJobEntity>> GetPendingJobsAsync(int limit = 100, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a count of OTA jobs grouped by status for the specified rollout.
        /// </summary>
        /// <param name="rolloutId">The rollout identifier to scope the count to.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Dictionary mapping OtaJobStatus to count.</returns>
        Task<Dictionary<OtaJobStatus, long>> CountByStatusAsync(string rolloutId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts multiple OTA job documents atomically in a single bulk write operation.
        /// </summary>
        /// <param name="jobs">The list of OTA job entities to insert.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task BulkInsertAsync(List<OtaJobEntity> jobs, CancellationToken cancellationToken = default);
    }
}
