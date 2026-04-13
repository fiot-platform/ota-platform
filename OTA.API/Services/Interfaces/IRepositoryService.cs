using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OTA.API.Models.DTOs;

namespace OTA.API.Services.Interfaces
{
    /// <summary>
    /// Service interface for Gitea repository registration, synchronisation, and lifecycle management.
    /// </summary>
    public interface IRepositoryService
    {
        /// <summary>
        /// Registers a new Gitea repository in the platform, validates the connection to Gitea,
        /// syncs initial metadata, and logs the audit event.
        /// </summary>
        /// <param name="request">The repository registration request including Gitea owner, repo name, and project association.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The registered repository DTO.</returns>
        Task<RepositoryDto> RegisterRepositoryAsync(RegisterRepositoryRequest request, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates mutable fields of a registered repository and logs the change.
        /// </summary>
        /// <param name="repositoryId">The identifier of the repository to update.</param>
        /// <param name="request">The update request payload.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated repository DTO.</returns>
        Task<RepositoryDto> UpdateRepositoryAsync(string repositoryId, UpdateRepositoryRequest request, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Triggers a full metadata synchronisation from Gitea for the specified repository.
        /// </summary>
        /// <param name="repositoryId">The identifier of the repository to synchronise.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SyncFromGiteaAsync(string repositoryId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a repository by its internal identifier.
        /// </summary>
        /// <param name="repositoryId">The repository identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The repository DTO, or null if not found.</returns>
        Task<RepositoryDto?> GetRepositoryByIdAsync(string repositoryId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all registered repositories with optional text filter and pagination.
        /// </summary>
        /// <param name="filter">Optional text filter matching repository name or URL.</param>
        /// <param name="page">One-based page number.</param>
        /// <param name="pageSize">Number of results per page.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of repository DTOs.</returns>
        Task<List<RepositoryDto>> GetRepositoriesAsync(string filter, int page, int pageSize, string? projectId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all repositories associated with the given project.
        /// </summary>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of repository DTOs for the project.</returns>
        Task<List<RepositoryDto>> GetByProjectIdAsync(string projectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deactivates a repository preventing further webhook processing and firmware syncs.
        /// </summary>
        /// <param name="repositoryId">The identifier of the repository to deactivate.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeactivateRepositoryAsync(string repositoryId, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);
    }
}
