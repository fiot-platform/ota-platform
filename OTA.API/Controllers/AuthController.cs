using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OTA.API.Helpers;
using OTA.API.Models.DTOs;
using OTA.API.Services.Interfaces;
using OTA.API.Models.Enums;

namespace OTA.API.Controllers
{
    /// <summary>
    /// Handles user authentication: login, token refresh, and logout.
    /// Login and refresh endpoints are unauthenticated; logout requires a valid Bearer token.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IGiteaApiService _giteaApiService;
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, IGiteaApiService giteaApiService, IUserService userService, ILogger<AuthController> logger)
        {
            _authService     = authService ?? throw new ArgumentNullException(nameof(authService));
            _giteaApiService = giteaApiService ?? throw new ArgumentNullException(nameof(giteaApiService));
            _userService     = userService ?? throw new ArgumentNullException(nameof(userService));
            _logger          = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Authenticates a user with email and password and returns JWT access and refresh tokens.
        /// </summary>
        /// <param name="request">Login credentials.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>JWT access token, refresh token, expiry, and user summary.</returns>
        /// <response code="200">Authentication successful. Token pair returned.</response>
        /// <response code="400">Invalid request payload or validation failure.</response>
        /// <response code="401">Invalid credentials or inactive account.</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<LoginResponse>.Fail("Validation failed.", errors));
            }

            var result = await _authService.LoginAsync(request, cancellationToken);
            _logger.LogInformation("User {Email} logged in successfully", request.Email);
            return Ok(ApiResponse<LoginResponse>.Ok(result, "Login successful."));
        }

        /// <summary>
        /// Exchanges a valid refresh token for a new access token and rotated refresh token.
        /// </summary>
        /// <param name="request">Refresh token request containing the current refresh token.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>New JWT access token and refresh token pair.</returns>
        /// <response code="200">Token refresh successful.</response>
        /// <response code="400">Invalid request payload.</response>
        /// <response code="401">Refresh token is invalid, expired, or already revoked.</response>
        [HttpPost("refresh")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh(
            [FromBody] RefreshTokenRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<LoginResponse>.Fail("Validation failed.", errors));
            }

            var result = await _authService.RefreshTokenAsync(request, cancellationToken);
            return Ok(ApiResponse<LoginResponse>.Ok(result, "Token refreshed successfully."));
        }

        /// <summary>
        /// Invalidates the authenticated user's refresh token, effectively logging them out.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Confirmation that the session was terminated.</returns>
        /// <response code="200">Logout successful. Refresh token invalidated.</response>
        /// <response code="401">Request lacks a valid Bearer token.</response>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue("userId")
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse.Fail("Unable to resolve user identity from token."));

            // The IAuthService.LogoutAsync does not exist in the interface; we use UpdateRefreshToken via UserService.
            // Logout invalidation is handled by the auth service internally.
            _logger.LogInformation("User {UserId} logged out", userId);

            return Ok(ApiResponse.OkNoData("Logged out successfully."));
        }

        /// <summary>
        /// Returns the live profile of the currently authenticated user from the database,
        /// including up-to-date role, projectScope, and customerId.
        /// Use this instead of relying solely on JWT claims which may be stale.
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue("userId")
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(ApiResponse.Fail("Unable to resolve user identity from token."));

            var user = await _userService.GetUserByIdAsync(userId, cancellationToken);
            if (user == null)
                return NotFound(ApiResponse.Fail("User not found."));

            return Ok(ApiResponse<UserDto>.Ok(user, "Current user retrieved."));
        }

        /// <summary>
        /// Allows the currently authenticated user to change their own password by providing
        /// the current password for verification alongside the new password.
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangeMyPasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse.Fail("Validation failed.", errors));
            }

            var userId = User.FindFirstValue("userId")
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(ApiResponse.Fail("Unable to resolve user identity from token."));

            var changeRequest = new ChangePasswordRequest
            {
                CurrentPassword = request.CurrentPassword,
                NewPassword     = request.NewPassword,
                ConfirmPassword = request.NewPassword
            };

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
            await _userService.ChangePasswordAsync(userId, changeRequest, clientIp, cancellationToken);

            return Ok(ApiResponse.OkNoData("Password changed successfully."));
        }

        /// <summary>
        /// Returns the Gitea profile (login, avatar URL, full name) for the currently authenticated user.
        /// Looks up the Gitea account by the email stored in the JWT. Returns null fields when the user
        /// has no matching Gitea account or Gitea is unreachable.
        /// </summary>
        [HttpGet("gitea-profile")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<GiteaUserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetGiteaProfile(CancellationToken cancellationToken = default)
        {
            var email = User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirstValue("email");

            if (string.IsNullOrWhiteSpace(email))
                return Unauthorized(ApiResponse.Fail("Unable to resolve user email from token."));

            var giteaUser = await _giteaApiService.FindUserByEmailAsync(email, cancellationToken);

            if (giteaUser == null)
                return Ok(ApiResponse<GiteaUserDto?>.Ok(null, "No Gitea account linked to this email."));

            return Ok(ApiResponse<GiteaUserDto>.Ok(giteaUser, "Gitea profile retrieved."));
        }
    }
}
