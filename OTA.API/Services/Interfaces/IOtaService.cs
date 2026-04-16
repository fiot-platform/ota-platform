using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OTA.API.Models.DTOs;

namespace OTA.API.Services.Interfaces
{
    /// <summary>
    /// Service interface for OTA rollout orchestration, device job management, and rollout lifecycle transitions.
    /// </summary>
    public interface IOtaService
    {
        /// <summary>
        /// Creates a new rollout in Draft status, resolves target devices by TargetType, and creates OtaJob documents
        /// for each device. Applies the configured rollout policy (canary %, batch size, concurrency limits).
        /// </summary>
        /// <param name="request">The rollout creation request including firmware, target, and policy details.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created rollout DTO.</returns>
        Task<RolloutDto> CreateRolloutAsync(CreateRolloutRequest request, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Transitions a rollout from Draft to Active, enabling its OTA jobs for dispatch.
        /// </summary>
        /// <param name="rolloutId">The rollout identifier.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated rollout DTO.</returns>
        Task<RolloutDto> StartRolloutAsync(string rolloutId, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pauses an Active rollout, halting dispatch of new OTA jobs while in-progress jobs continue.
        /// </summary>
        /// <param name="rolloutId">The rollout identifier.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated rollout DTO.</returns>
        Task<RolloutDto> PauseRolloutAsync(string rolloutId, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resumes a Paused rollout, re-enabling dispatch of pending OTA jobs.
        /// </summary>
        /// <param name="rolloutId">The rollout identifier.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated rollout DTO.</returns>
        Task<RolloutDto> ResumeRolloutAsync(string rolloutId, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels an Active or Paused rollout, marking pending jobs as Cancelled and updating rollout status.
        /// </summary>
        /// <param name="rolloutId">The rollout identifier.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated rollout DTO.</returns>
        Task<RolloutDto> CancelRolloutAsync(string rolloutId, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a rollout by its internal identifier.
        /// </summary>
        /// <param name="rolloutId">The rollout identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The rollout DTO, or null if not found.</returns>
        Task<RolloutDto?> GetRolloutByIdAsync(string rolloutId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a paginated list of rollouts with optional text filter.
        /// </summary>
        /// <param name="filter">Optional text filter matching rollout name.</param>
        /// <param name="page">One-based page number.</param>
        /// <param name="pageSize">Number of results per page.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Paged result containing rollout DTOs and total count.</returns>
        Task<PagedResult<RolloutDto>> GetRolloutsAsync(string filter, int page, int pageSize, string? projectId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all OTA jobs belonging to the specified rollout.
        /// </summary>
        /// <param name="rolloutId">The rollout identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of OTA job DTOs.</returns>
        Task<List<OtaJobDto>> GetRolloutJobsAsync(string rolloutId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resets a failed OTA job to Pending status for re-dispatch on the next dispatch cycle.
        /// </summary>
        /// <param name="jobId">The OTA job identifier to retry.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated OTA job DTO.</returns>
        Task<OtaJobDto> RetryJobAsync(string jobId, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a summary of rollout progress including job counts, success rate, and current phase.
        /// </summary>
        /// <param name="rolloutId">The rollout identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The rollout summary DTO.</returns>
        Task<RolloutSummaryDto> GetRolloutSummaryAsync(string rolloutId, CancellationToken cancellationToken = default);
    }
}
