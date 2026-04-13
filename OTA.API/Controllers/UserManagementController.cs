using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OTA.API.Helpers;
using OTA.API.Models.DTOs;
using OTA.API.Models.Enums;
using OTA.API.Services.Interfaces;

namespace OTA.API.Controllers
{
    /// <summary>
    /// User management endpoints for creating, updating, activating/deactivating, and assigning roles.
    /// All endpoints require authentication. Role-based authorization enforced per action.
    /// </summary>
    [ApiController]
    [Route("api/users")]
    [Authorize]
    [Produces("application/json")]
    public class UserManagementController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserManagementController> _logger;

        public UserManagementController(IUserService userService, ILogger<UserManagementController> logger)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _logger      = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private string CurrentUserId =>
            User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        private string CurrentEmail =>
            User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? string.Empty;

        private UserRole CurrentRole =>
            Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role"), out var r) ? r : UserRole.Viewer;

        private string ClientIp =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

        /// <summary>
        /// Returns a paginated list of all platform users. SuperAdmin and PlatformAdmin only.
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "CanManageUsers")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<UserDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsers(
            [FromQuery] string? filter = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            var result = await _userService.GetUsersAsync(filter ?? string.Empty, page, pageSize, cancellationToken);
            var pagination = PaginationInfo.Create(page, pageSize, result.TotalCount);
            return Ok(ApiResponse<List<UserDto>>.Ok(result.Items, "Users retrieved successfully.", pagination));
        }

        /// <summary>
        /// Returns a single user by their platform UserId. SuperAdmin and PlatformAdmin only.
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "CanManageUsers")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserById(string id, CancellationToken cancellationToken = default)
        {
            var user = await _userService.GetUserByIdAsync(id, cancellationToken);
            if (user == null)
                return NotFound(ApiResponse<UserDto>.Fail($"User '{id}' was not found."));

            return Ok(ApiResponse<UserDto>.Ok(user));
        }

        /// <summary>
        /// Creates a new user account. SuperAdmin only can create Admin-level roles; PlatformAdmin for lower roles.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "CanManageUsers")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateUser(
            [FromBody] CreateUserRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<UserDto>.Fail("Validation failed.", errors));
            }

            var created = await _userService.CreateUserAsync(
                request, CurrentUserId, CurrentEmail, CurrentRole, ClientIp, cancellationToken);

            return CreatedAtAction(nameof(GetUserById), new { id = created.UserId },
                ApiResponse<UserDto>.Ok(created, "User created successfully."));
        }

        /// <summary>
        /// Updates an existing user's mutable fields (name, email, project scope).
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "CanManageUsers")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUser(
            string id,
            [FromBody] UpdateUserRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<UserDto>.Fail("Validation failed.", errors));
            }

            var updated = await _userService.UpdateUserAsync(id, request, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
            return Ok(ApiResponse<UserDto>.Ok(updated, "User updated successfully."));
        }

        /// <summary>
        /// Soft-deactivates a user account. SuperAdmin only.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeactivateUser(string id, CancellationToken cancellationToken = default)
        {
            await _userService.DeactivateUserAsync(id, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
            return Ok(ApiResponse.OkNoData("User deactivated successfully."));
        }

        /// <summary>
        /// Re-activates a previously deactivated user account. SuperAdmin and PlatformAdmin.
        /// </summary>
        [HttpPost("{id}/activate")]
        [Authorize(Policy = "CanManageUsers")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ActivateUser(string id, CancellationToken cancellationToken = default)
        {
            await _userService.ActivateUserAsync(id, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
            return Ok(ApiResponse.OkNoData("User activated successfully."));
        }

        /// <summary>
        /// Assigns a new role to a user. SuperAdmin only.
        /// </summary>
        [HttpPost("{id}/assign-role")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignRole(
            string id,
            [FromBody] AssignRoleRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse.Fail("Validation failed.", errors));
            }

            await _userService.AssignRoleAsync(id, request.Role, CurrentUserId, CurrentEmail, CurrentRole, ClientIp, cancellationToken);
            return Ok(ApiResponse.OkNoData($"Role '{request.Role}' assigned successfully."));
        }
    }

    /// <summary>Request body for role assignment action.</summary>
    public class AssignRoleRequest
    {
        public UserRole Role { get; set; }
    }
}
