using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OTA.API.Models.DTOs;

namespace OTA.API.Services.Interfaces
{
    /// <summary>
    /// Service interface for project lifecycle management operations.
    /// </summary>
    public interface IProjectService
    {
        /// <summary>
        /// Creates a new project and logs the audit event.
        /// </summary>
        /// <param name="request">The project creation request payload.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created project DTO.</returns>
        Task<ProjectDto> CreateProjectAsync(CreateProjectRequest request, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates mutable fields of an existing project and logs the change.
        /// </summary>
        /// <param name="projectId">The identifier of the project to update.</param>
        /// <param name="request">The update request payload.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated project DTO.</returns>
        Task<ProjectDto> UpdateProjectAsync(string projectId, UpdateProjectRequest request, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Activates a project making it available for repository and rollout operations.
        /// </summary>
        /// <param name="projectId">The identifier of the project to activate.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ActivateProjectAsync(string projectId, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deactivates a project preventing new rollouts while preserving historical data.
        /// </summary>
        /// <param name="projectId">The identifier of the project to deactivate.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeactivateProjectAsync(string projectId, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a project by its identifier.
        /// </summary>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The project DTO, or null if not found.</returns>
        Task<ProjectDto?> GetProjectByIdAsync(string projectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a paginated list of all projects, optionally filtered by name.
        /// </summary>
        /// <param name="filter">Optional text filter.</param>
        /// <param name="page">One-based page number.</param>
        /// <param name="pageSize">Number of results per page.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Paged result containing project DTOs and total count.</returns>
        Task<PagedResult<ProjectDto>> GetProjectsAsync(string filter, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all projects belonging to the specified customer.
        /// </summary>
        /// <param name="customerId">The customer identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of project DTOs for the customer.</returns>
        Task<List<ProjectDto>> GetProjectsByCustomerAsync(string customerId, CancellationToken cancellationToken = default);
    }
}
