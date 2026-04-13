using System.Threading;
using System.Threading.Tasks;
using OTA.API.Models.DTOs;
using OTA.API.Models.Entities;

namespace OTA.API.Services.Interfaces
{
    /// <summary>
    /// Service interface for authentication operations including login, token refresh, and password management.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Authenticates a user with email and password credentials and returns a JWT access token with refresh token.
        /// </summary>
        /// <param name="request">The login credentials including email and password.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="LoginResponse"/> containing access token, refresh token, and user profile information.</returns>
        Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates an expired access token paired with a valid refresh token and issues a new token pair.
        /// </summary>
        /// <param name="request">The refresh token request containing the current refresh token.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A new <see cref="LoginResponse"/> with fresh access and refresh tokens.</returns>
        Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Hashes a plain-text password using BCrypt with a work factor appropriate for production use.
        /// </summary>
        /// <param name="password">The plain-text password to hash.</param>
        /// <returns>The BCrypt password hash string.</returns>
        string HashPassword(string password);

        /// <summary>
        /// Verifies a plain-text password against a stored BCrypt hash.
        /// </summary>
        /// <param name="password">The plain-text password to verify.</param>
        /// <param name="hash">The BCrypt hash to verify against.</param>
        /// <returns>True if the password matches the hash; otherwise false.</returns>
        bool VerifyPassword(string password, string hash);

        /// <summary>
        /// Generates a signed JWT access token for the specified user, embedding role, email, customerId, and project scope claims.
        /// </summary>
        /// <param name="user">The authenticated user entity whose claims are embedded in the token.</param>
        /// <returns>A signed JWT token string.</returns>
        string GenerateJwtToken(UserEntity user);
    }
}
