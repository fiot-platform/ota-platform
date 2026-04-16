using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OTA.API.Models.Entities;

namespace OTA.API.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for ProjectEntity providing project-specific query operations.
    /// </summary>
    public interface IProjectRepository : IBaseRepository<ProjectEntity>
    {
        /// <summary>
        /// Retrieves all projects belonging to the specified customer.
        /// </summary>
        /// <param name="customerId">The customer identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of projects for the customer.</returns>
        Task<List<ProjectEntity>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all currently active projects.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of active projects.</returns>
        Task<List<ProjectEntity>> GetActiveProjectsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches projects by name or description with pagination.
        /// </summary>
        /// <param name="filter">Search text to match against project name or description.</param>
        /// <param name="page">One-based page number.</param>
        /// <param name="pageSize">Number of results per page.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Paged list of matching projects.</returns>
        Task<List<ProjectEntity>> SearchAsync(string filter, int page, int pageSize, List<string>? allowedProjectIds = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Counts projects matching the specified filter string.
        /// </summary>
        /// <param name="filter">Search text to match against project name or description.</param>
        /// <param name="allowedProjectIds">When non-null, restricts results to the given project IDs (project scope).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Total number of matching projects.</returns>
        Task<long> CountAsync(string filter, List<string>? allowedProjectIds = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a project by its platform GUID (projectId field), not MongoDB _id.
        /// </summary>
        /// <param name="projectId">The platform GUID of the project.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The matching project entity, or null if not found.</returns>
        Task<ProjectEntity?> GetByProjectIdAsync(string projectId, CancellationToken cancellationToken = default);
    }
}
