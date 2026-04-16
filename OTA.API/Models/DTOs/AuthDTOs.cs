using System.ComponentModel.DataAnnotations;
using OTA.API.Models.Enums;

namespace OTA.API.Models.DTOs
{
    // ─────────────────────────────────────────────────────────────────────────
    // Authentication DTOs
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Request body for the POST /api/auth/login endpoint.</summary>
    public sealed class LoginRequest
    {
        /// <summary>Registered email address used as the login identifier.</summary>
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "A valid email address is required.")]
        [MaxLength(254)]
        public string Email { get; set; } = string.Empty;

        /// <summary>Plain-text password.</summary>
        [Required(ErrorMessage = "Password is required.")]
        [MaxLength(128)]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>Response body returned by POST /api/auth/login on success.</summary>
    public sealed class LoginResponse
    {
        /// <summary>Short-lived JWT access token. Include in Authorization: Bearer header.</summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>Long-lived opaque refresh token for obtaining a new access token.</summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>UTC timestamp when the access token expires.</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>Authenticated user summary for UI bootstrapping.</summary>
        public UserDto User { get; set; } = new();
    }

    /// <summary>Request body for the POST /api/auth/refresh endpoint.</summary>
    public sealed class RefreshTokenRequest
    {
        /// <summary>Opaque refresh token previously issued by the login or refresh endpoint.</summary>
        [Required(ErrorMessage = "Refresh token is required.")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// Compact user representation embedded in login/refresh responses for UI bootstrapping.
    /// Contains only the fields needed by the frontend to render the navigation and access controls.
    /// </summary>
    public sealed class UserDto
    {
        /// <summary>Unique platform user identifier (MongoDB _id).</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Unique platform user identifier.</summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>Full display name of the user.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Alias for <see cref="Name"/> used by UserService.</summary>
        public string FullName
        {
            get => Name;
            set => Name = value;
        }

        /// <summary>Email address / login identifier.</summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>Assigned platform role as a string (matches <see cref="UserRole"/> enum names).</summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>Customer tenant the user belongs to. Null for platform-level roles.</summary>
        public string? CustomerId { get; set; }

        /// <summary>Whether the user account is active.</summary>
        public bool IsActive { get; set; }

        /// <summary>Optional phone number.</summary>
        public string? PhoneNumber { get; set; }

        /// <summary>Project scope list.</summary>
        public List<string> ProjectScope { get; set; } = new();

        /// <summary>UTC timestamp when the account was created.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>UTC timestamp of the most recent update.</summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>UTC timestamp of the user's most recent successful login. Null if never logged in.</summary>
        public DateTime? LastLoginAt { get; set; }
    }

    /// <summary>Request body for the POST /api/auth/change-password endpoint (self-service).</summary>
    public sealed class ChangeMyPasswordRequest
    {
        /// <summary>The user's current password for verification.</summary>
        [Required(ErrorMessage = "Current password is required.")]
        public string CurrentPassword { get; set; } = string.Empty;

        /// <summary>The desired new password.</summary>
        [Required(ErrorMessage = "New password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        [MaxLength(128)]
        public string NewPassword { get; set; } = string.Empty;
    }

}
