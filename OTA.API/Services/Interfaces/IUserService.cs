using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OTA.API.Models.DTOs;
using OTA.API.Models.Enums;

namespace OTA.API.Services.Interfaces
{
    /// <summary>
    /// Service interface for user lifecycle management including creation, role assignment, and deactivation.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Creates a new user account, validates role constraints, checks email uniqueness, and logs the audit event.
        /// </summary>
        /// <param name="request">The user creation request containing name, email, role, and customer details.</param>
        /// <param name="callerUserId">The identifier of the authenticated user performing the operation.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="callerRole">The role of the authenticated caller for role-constraint validation.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created user's profile DTO.</returns>
        Task<UserDto> CreateUserAsync(CreateUserRequest request, string callerUserId, string callerEmail, UserRole callerRole, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates mutable fields (name, phone, metadata) of an existing user account and logs the change.
        /// </summary>
        /// <param name="userId">The identifier of the user to update.</param>
        /// <param name="request">The update request payload.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated user profile DTO.</returns>
        Task<UserDto> UpdateUserAsync(string userId, UpdateUserRequest request, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Permanently deletes a user account. SuperAdmin only.
        /// </summary>
        Task DeleteUserAsync(string userId, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Soft-deactivates a user account, preventing login while preserving audit history.
        /// </summary>
        /// <param name="userId">The identifier of the user to deactivate.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeactivateUserAsync(string userId, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Re-activates a previously deactivated user account.
        /// </summary>
        /// <param name="userId">The identifier of the user to reactivate.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ActivateUserAsync(string userId, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a user profile by identifier.
        /// </summary>
        /// <param name="userId">The identifier of the user to retrieve.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The user profile DTO, or null if not found.</returns>
        Task<UserDto?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a paginated, optionally filtered list of users.
        /// </summary>
        /// <param name="filter">Optional text filter to match against name or email.</param>
        /// <param name="page">One-based page number.</param>
        /// <param name="pageSize">Number of results per page.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A paged result containing user DTOs and total count.</returns>
        Task<PagedResult<UserDto>> GetUsersAsync(string filter, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Changes the role of an existing user, enforcing caller-role constraints.
        /// </summary>
        /// <param name="userId">The identifier of the user whose role is to be changed.</param>
        /// <param name="newRole">The new role to assign.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="callerRole">The role of the authenticated caller for constraint checking.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task AssignRoleAsync(string userId, UserRole newRole, string callerUserId, string callerEmail, UserRole callerRole, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Changes the password of a user after verifying the current password.
        /// </summary>
        /// <param name="userId">The identifier of the user changing their password.</param>
        /// <param name="request">The change-password request containing current and new password.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ChangePasswordAsync(string userId, ChangePasswordRequest request, string ipAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all users with the specified role.
        /// </summary>
        /// <param name="role">The role to filter by.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of user DTOs with the specified role.</returns>
        Task<List<UserDto>> GetUsersByRoleAsync(UserRole role, CancellationToken cancellationToken = default);

        /// <summary>
        /// Replaces the user's project scope with the provided list of project IDs.
        /// Logs an audit event capturing the before and after state.
        /// </summary>
        /// <param name="userId">The identifier of the user to update.</param>
        /// <param name="projectIds">The new list of project IDs to assign. Empty list clears all restrictions.</param>
        /// <param name="callerUserId">The identifier of the authenticated caller.</param>
        /// <param name="callerEmail">The email of the authenticated caller for audit logging.</param>
        /// <param name="callerRole">The role of the authenticated caller.</param>
        /// <param name="ipAddress">The caller's IP address for audit context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated user profile DTO.</returns>
        Task<UserDto> AssignProjectsAsync(
            string userId,
            List<string> projectIds,
            string callerUserId,
            string callerEmail,
            UserRole callerRole,
            string ipAddress,
            CancellationToken cancellationToken = default);
    }
}
