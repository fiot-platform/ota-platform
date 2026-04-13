using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OTA.API.Models.DTOs;

namespace OTA.API.Services.Interfaces
{
    /// <summary>
    /// Service interface for rollout policy CRUD operations.
    /// Rollout policies control canary percentages, batch sizes, concurrency limits, and downgrade permissions.
    /// </summary>
    public interface IRolloutPolicyService
    {
        /// <summary>
        /// Creates a new rollout policy and persists it.
        /// </summary>
        /// <param name="request">The policy creation request.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created rollout policy DTO.</returns>
        Task<RolloutPolicyDto> CreatePolicyAsync(CreateRolloutPolicyRequest request, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing rollout policy.
        /// </summary>
        /// <param name="policyId">The identifier of the policy to update.</param>
        /// <param name="request">The update request payload.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated rollout policy DTO.</returns>
        Task<RolloutPolicyDto> UpdatePolicyAsync(string policyId, UpdateRolloutPolicyRequest request, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a rollout policy by its identifier.
        /// </summary>
        /// <param name="policyId">The policy identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The rollout policy DTO, or null if not found.</returns>
        Task<RolloutPolicyDto?> GetPolicyByIdAsync(string policyId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all active rollout policies.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of active rollout policy DTOs.</returns>
        Task<List<RolloutPolicyDto>> GetActivePoliciesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a rollout policy. Policies in use by active rollouts cannot be deleted.
        /// </summary>
        /// <param name="policyId">The identifier of the policy to delete.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeletePolicyAsync(string policyId, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);
    }
}
