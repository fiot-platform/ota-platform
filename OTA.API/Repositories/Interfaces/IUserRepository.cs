using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OTA.API.Models.Entities;
using OTA.API.Models.Enums;

namespace OTA.API.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for UserEntity providing user-specific query and mutation operations.
    /// </summary>
    public interface IUserRepository : IBaseRepository<UserEntity>
    {
        /// <summary>
        /// Retrieves a user by their email address.
        /// </summary>
        /// <param name="email">The email address to search for (case-insensitive).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The matching user entity, or null if not found.</returns>
        Task<UserEntity?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all users with the specified role.
        /// </summary>
        /// <param name="role">The role to filter by.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of users with the specified role.</returns>
        Task<List<UserEntity>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all users associated with the given customer.
        /// </summary>
        /// <param name="customerId">The customer identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of users belonging to the customer.</returns>
        Task<List<UserEntity>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the hashed password for the specified user.
        /// </summary>
        /// <param name="userId">The user's unique identifier.</param>
        /// <param name="passwordHash">The new bcrypt password hash.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UpdatePasswordAsync(string userId, string passwordHash, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the refresh token and its expiry for the specified user.
        /// </summary>
        /// <param name="userId">The user's unique identifier.</param>
        /// <param name="refreshToken">The new refresh token value (null to clear).</param>
        /// <param name="expiresAt">The refresh token expiry date-time in UTC.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task UpdateRefreshTokenAsync(string userId, string? refreshToken, System.DateTime expiresAt, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches users based on a free-text filter with pagination.
        /// </summary>
        /// <param name="filter">Text to match against name, email, or other searchable fields.</param>
        /// <param name="page">One-based page number.</param>
        /// <param name="pageSize">Number of results per page.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Paged list of matching users.</returns>
        Task<List<UserEntity>> SearchUsersAsync(string filter, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Counts users matching the given filter string.
        /// </summary>
        /// <param name="filter">Text to match against searchable fields.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Total number of matching users.</returns>
        Task<long> CountAsync(string filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a user as inactive (soft delete).
        /// </summary>
        /// <param name="userId">The unique identifier of the user to deactivate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeactivateAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a user by their current refresh token value.
        /// </summary>
        /// <param name="refreshToken">The refresh token value to look up.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The matching user entity, or null if not found.</returns>
        Task<UserEntity?> FindByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    }
}
