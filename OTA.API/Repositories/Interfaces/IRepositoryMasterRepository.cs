using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OTA.API.Models.Entities;

namespace OTA.API.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for RepositoryMasterEntity providing Gitea repository metadata query operations.
    /// </summary>
    public interface IRepositoryMasterRepository : IBaseRepository<RepositoryMasterEntity>
    {
        /// <summary>
        /// Retrieves all repositories linked to the specified project.
        /// </summary>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of repositories for the project.</returns>
        Task<List<RepositoryMasterEntity>> GetByProjectIdAsync(string projectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the repository linked to the specified Gitea repository identifier.
        /// </summary>
        /// <param name="giteaRepoId">The Gitea repository ID (numeric or string identifier from Gitea API).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The matching repository entity, or null if not found.</returns>
        Task<RepositoryMasterEntity?> GetByGiteaRepoIdAsync(string giteaRepoId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all active (enabled) repositories.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of active repository entities.</returns>
        Task<List<RepositoryMasterEntity>> GetActiveAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches repositories by name or URL with pagination.
        /// </summary>
        /// <param name="filter">Search text to match against repository name or URL.</param>
        /// <param name="page">One-based page number.</param>
        /// <param name="pageSize">Number of results per page.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Paged list of matching repositories.</returns>
        Task<List<RepositoryMasterEntity>> SearchAsync(string filter, int page, int pageSize, List<string>? allowedProjectIds = null, CancellationToken cancellationToken = default);
    }
}
