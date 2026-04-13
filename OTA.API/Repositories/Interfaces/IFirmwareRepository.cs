using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OTA.API.Models.Entities;
using OTA.API.Models.Enums;

namespace OTA.API.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for FirmwareVersionEntity providing firmware-specific query operations.
    /// </summary>
    public interface IFirmwareRepository : IBaseRepository<FirmwareVersionEntity>
    {
        /// <summary>
        /// Retrieves all firmware entries linked to the specified Gitea repository.
        /// </summary>
        /// <param name="repositoryId">The internal repository master identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of firmware entities for the repository.</returns>
        Task<List<FirmwareVersionEntity>> GetByRepositoryIdAsync(string repositoryId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all firmware entries associated with the given project.
        /// </summary>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of firmware entities for the project.</returns>
        Task<List<FirmwareVersionEntity>> GetByProjectIdAsync(string projectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all firmware entries with the specified status.
        /// </summary>
        /// <param name="status">The firmware lifecycle status to filter by.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of firmware entities with the specified status.</returns>
        Task<List<FirmwareVersionEntity>> GetByStatusAsync(FirmwareStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all firmware entries assigned to the specified release channel.
        /// </summary>
        /// <param name="channel">The firmware release channel.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of firmware entities in the specified channel.</returns>
        Task<List<FirmwareVersionEntity>> GetByChannelAsync(FirmwareChannel channel, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves approved firmware entries compatible with the specified device model,
        /// hardware revision, and release channel.
        /// </summary>
        /// <param name="model">The device model identifier.</param>
        /// <param name="hardwareRevision">The device hardware revision string.</param>
        /// <param name="channel">The release channel to search within.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of approved firmware entities compatible with the device.</returns>
        Task<List<FirmwareVersionEntity>> GetApprovedForModelAsync(string model, string hardwareRevision, FirmwareChannel channel, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a firmware entity by its platform-assigned FirmwareId (GUID), not the MongoDB _id.
        /// </summary>
        Task<FirmwareVersionEntity?> GetByFirmwareIdAsync(string firmwareId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches firmware entries by version, model, or description with pagination.
        /// </summary>
        Task<List<FirmwareVersionEntity>> SearchAsync(string filter, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches firmware with optional status, channel, project, repository, and text filters.
        /// Returns both the items for the requested page and the total matching count.
        /// </summary>
        Task<(List<FirmwareVersionEntity> Items, long TotalCount)> SearchWithFiltersAsync(
            string? search,
            string? status,
            string? channel,
            string? projectId,
            string? repositoryId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns a count of firmware entries grouped by status for dashboard and reporting.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Dictionary mapping FirmwareStatus to count.</returns>
        Task<Dictionary<FirmwareStatus, long>> CountByStatusAsync(CancellationToken cancellationToken = default);
    }
}
