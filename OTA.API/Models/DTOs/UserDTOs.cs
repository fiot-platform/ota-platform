using System.ComponentModel.DataAnnotations;
using OTA.API.Models.Enums;

namespace OTA.API.Models.DTOs
{
    // ─────────────────────────────────────────────────────────────────────────
    // User Management DTOs
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Request body for creating a new platform user account.</summary>
    public sealed class CreateUserRequest
    {
        /// <summary>Full display name (first + last).</summary>
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Unique email address; used as login identifier.</summary>
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        [MaxLength(254)]
        public string Email { get; set; } = string.Empty;

        /// <summary>Plain-text initial password. Will be BCrypt-hashed before persistence.</summary>
        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        [MaxLength(128)]
        public string Password { get; set; } = string.Empty;

        /// <summary>Platform role to assign. Must match a valid <see cref="UserRole"/> value.</summary>
        [Required(ErrorMessage = "Role is required.")]
        public UserRole Role { get; set; }

        /// <summary>Customer tenant identifier. Required for CustomerAdmin and below.</summary>
        [MaxLength(36)]
        public string? CustomerId { get; set; }

        /// <summary>
        /// Optional list of ProjectId values scoping this user's data access.
        /// Empty list means no additional project restrictions within their customer scope.
        /// </summary>
        public List<string> ProjectScope { get; set; } = new();

        /// <summary>Whether the new account is immediately active. Defaults to true.</summary>
        public bool IsActive { get; set; } = true;
    }

    /// <summary>Request body for updating an existing user account.</summary>
    public sealed class UpdateUserRequest
    {
        /// <summary>Updated full display name.</summary>
        [MaxLength(150)]
        public string? Name { get; set; }

        /// <summary>Updated email address. Must remain unique across the platform.</summary>
        [EmailAddress]
        [MaxLength(254)]
        public string? Email { get; set; }

        /// <summary>New plain-text password. Only provided when the caller wants to change the password.</summary>
        [MinLength(8)]
        [MaxLength(128)]
        public string? Password { get; set; }

        /// <summary>Updated customer tenant assignment.</summary>
        [MaxLength(36)]
        public string? CustomerId { get; set; }

        /// <summary>Replacement project scope list.</summary>
        public List<string>? ProjectScope { get; set; }

        /// <summary>Active / inactive state toggle.</summary>
        public bool? IsActive { get; set; }
    }

    /// <summary>Request body for assigning or changing a user's platform role.</summary>
    public sealed class AssignRoleRequest
    {
        /// <summary>The new role to assign to the target user.</summary>
        [Required(ErrorMessage = "Role is required.")]
        public UserRole Role { get; set; }
    }

    /// <summary>
    /// Summary row item used in paginated user list responses.
    /// Contains only the fields needed to render a user table row.
    /// </summary>
    public sealed class UserListDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? CustomerId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        /// <summary>Total count of matching users (for pagination metadata).</summary>
        public long TotalCount { get; set; }
    }

    /// <summary>Full user detail returned by the GET /api/users/{userId} endpoint.</summary>
    public sealed class UserDetailDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? CustomerId { get; set; }
        public List<string> ProjectScope { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    /// <summary>Wrapper for paginated user list responses.</summary>
    public sealed class PagedUserListResponse
    {
        public List<UserListDto> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public long TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public sealed class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Current password is required.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        [MaxLength(128)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password confirmation is required.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
