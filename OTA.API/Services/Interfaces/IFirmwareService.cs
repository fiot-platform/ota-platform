using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OTA.API.Models.DTOs;
using OTA.API.Models.Enums;

namespace OTA.API.Services.Interfaces
{
    /// <summary>
    /// Service interface for firmware lifecycle management including the approval workflow.
    /// Workflow states: Draft -> PendingQA -> QAVerified -> PendingApproval -> Approved | Rejected.
    /// </summary>
    public interface IFirmwareService
    {
        /// <summary>
        /// Creates a new firmware record in Draft status from a manual upload or sync trigger.
        /// </summary>
        /// <param name="request">The firmware creation request including version, repository, and model compatibility data.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created firmware DTO.</returns>
        Task<FirmwareDto> CreateFirmwareAsync(CreateFirmwareRequest request, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates mutable metadata fields of a firmware record that has not yet been approved.
        /// </summary>
        /// <param name="firmwareId">The identifier of the firmware to update.</param>
        /// <param name="request">The update request payload.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated firmware DTO.</returns>
        Task<FirmwareDto> UpdateFirmwareAsync(string firmwareId, UpdateFirmwareRequest request, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Transitions firmware from PendingApproval to Approved status. Only authorized approvers may call this.
        /// </summary>
        /// <param name="id">The firmware identifier.</param>
        /// <param name="userId">The approver's user identifier.</param>
        /// <param name="notes">Optional approval notes.</param>
        /// <param name="ipAddress">The approver's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The approved firmware DTO.</returns>
        Task<FirmwareDto> ApproveFirmwareAsync(string id, string userId, string notes, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Transitions firmware from PendingApproval to Rejected status with a mandatory rejection reason.
        /// </summary>
        /// <param name="id">The firmware identifier.</param>
        /// <param name="userId">The rejector's user identifier.</param>
        /// <param name="reason">The mandatory rejection reason.</param>
        /// <param name="ipAddress">The rejector's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The rejected firmware DTO.</returns>
        Task<FirmwareDto> RejectFirmwareAsync(string id, string userId, string reason, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Transitions firmware from PendingQA to QAVerified status. Only QA team members may call this.
        /// </summary>
        /// <param name="id">The firmware identifier.</param>
        /// <param name="userId">The QA engineer's user identifier.</param>
        /// <param name="remarks">Optional QA verification remarks.</param>
        /// <param name="ipAddress">The QA engineer's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The QA-verified firmware DTO.</returns>
        Task<FirmwareDto> QAVerifyFirmwareAsync(string id, string userId, string remarks, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Assigns the firmware to a release channel (e.g. Stable, Beta, Canary).
        /// </summary>
        /// <param name="firmwareId">The firmware identifier.</param>
        /// <param name="channel">The release channel to assign.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated firmware DTO.</returns>
        Task<FirmwareDto> AssignChannelAsync(string firmwareId, FirmwareChannel channel, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a firmware record by its identifier.
        /// </summary>
        /// <param name="firmwareId">The firmware identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The firmware DTO, or null if not found.</returns>
        Task<FirmwareDto?> GetFirmwareByIdAsync(string firmwareId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a paginated, filtered list of firmware records.
        /// </summary>
        Task<PagedResult<FirmwareDto>> GetFirmwareListAsync(
            string search,
            string? status,
            string? channel,
            string? projectId,
            string? repositoryId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Synchronises firmware releases from Gitea for the specified repository, creating new Draft records
        /// for tags or releases not yet tracked in the platform.
        /// </summary>
        /// <param name="repositoryId">The internal repository master identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Number of new firmware records created.</returns>
        Task<int> SyncFirmwareFromGiteaAsync(string repositoryId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks an approved firmware as Deprecated, making it ineligible for new rollouts but preserving existing jobs.
        /// </summary>
        /// <param name="firmwareId">The firmware identifier.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeprecateFirmwareAsync(string firmwareId, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);
    }
}
