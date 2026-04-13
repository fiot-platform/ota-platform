using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OTA.API.Repositories.Interfaces
{
    /// <summary>
    /// Generic base repository interface providing standard CRUD operations for all entities.
    /// </summary>
    /// <typeparam name="T">The entity type managed by this repository.</typeparam>
    public interface IBaseRepository<T> where T : class
    {
        /// <summary>
        /// Retrieves an entity by its unique identifier.
        /// </summary>
        /// <param name="id">The MongoDB ObjectId string of the entity.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The entity if found; otherwise null.</returns>
        Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all entities in the collection.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of all entities.</returns>
        Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts a new entity into the collection.
        /// </summary>
        /// <param name="entity">The entity to insert.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task InsertAsync(T entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Replaces an existing entity in the collection identified by its Id.
        /// </summary>
        /// <param name="id">The MongoDB ObjectId string of the entity to update.</param>
        /// <param name="entity">The updated entity.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UpdateAsync(string id, T entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an entity by its unique identifier.
        /// </summary>
        /// <param name="id">The MongoDB ObjectId string of the entity to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if an entity was deleted; otherwise false.</returns>
        Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks whether an entity with the specified identifier exists.
        /// </summary>
        /// <param name="id">The MongoDB ObjectId string to check.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the entity exists; otherwise false.</returns>
        Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);
    }
}
