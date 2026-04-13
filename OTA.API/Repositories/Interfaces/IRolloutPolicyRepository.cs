using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OTA.API.Models.Entities;

namespace OTA.API.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for RolloutPolicyEntity providing rollout policy query operations.
    /// </summary>
    public interface IRolloutPolicyRepository : IBaseRepository<RolloutPolicyEntity>
    {
        /// <summary>
        /// Retrieves all active rollout policies available for assignment.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of active rollout policy entities.</returns>
        Task<List<RolloutPolicyEntity>> GetActiveAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a rollout policy by its unique name.
        /// </summary>
        /// <param name="name">The policy name to search for.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The matching rollout policy entity, or null if not found.</returns>
        Task<RolloutPolicyEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    }
}
