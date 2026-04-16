using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OTA.API.Models.DTOs;
using OTA.API.Models.Entities;
using OTA.API.Models.Enums;
using OTA.API.Repositories.Interfaces;
using OTA.API.Services.Interfaces;

namespace OTA.API.Services
{
    /// <summary>
    /// Implements user lifecycle management including creation, role management, deactivation,
    /// and password changes. Enforces role-based constraints and emits audit events on every state change.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuthService _authService;
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<UserService> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <summary>
        /// Initialises a new instance of <see cref="UserService"/>.
        /// </summary>
        public UserService(
            IUserRepository userRepository,
            IAuthService authService,
            IAuditService auditService,
            INotificationService notificationService,
            ILogger<UserService> logger)
        {
            _userRepository      = userRepository      ?? throw new ArgumentNullException(nameof(userRepository));
            _authService         = authService         ?? throw new ArgumentNullException(nameof(authService));
            _auditService        = auditService        ?? throw new ArgumentNullException(nameof(auditService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger              = logger              ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<UserDto> CreateUserAsync(
            CreateUserRequest request,
            string callerUserId,
            string callerEmail,
            UserRole callerRole,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.Email)) throw new ArgumentException("Email is required.");
            if (string.IsNullOrWhiteSpace(request.Password)) throw new ArgumentException("Password is required.");
            if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Name is required.");

            // Role constraint: only SuperAdmin can create SuperAdmin accounts
            if (request.Role == UserRole.SuperAdmin && callerRole != UserRole.SuperAdmin)
                throw new UnauthorizedAccessException("Only a SuperAdmin can create another SuperAdmin account.");

            // CustomerAdmin can only create users within their own customer scope
            if (callerRole == UserRole.CustomerAdmin && request.CustomerId != null)
            {
                var caller = await _userRepository.GetByIdAsync(callerUserId, cancellationToken);
                if (caller?.CustomerId != request.CustomerId)
                    throw new UnauthorizedAccessException("CustomerAdmin can only create users within their own customer.");
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            var existing = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
            if (existing != null)
                throw new InvalidOperationException($"A user with email '{normalizedEmail}' already exists.");

            var user = new UserEntity
            {
                UserId = Guid.NewGuid().ToString(),
                Name = request.Name.Trim(),
                Email = normalizedEmail,
                PasswordHash = _authService.HashPassword(request.Password),
                Role = request.Role,
                CustomerId = string.IsNullOrWhiteSpace(request.CustomerId) ? null : request.CustomerId.Trim(),
                IsActive = request.IsActive,
                ProjectScope = request.ProjectScope ?? new List<string>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _userRepository.InsertAsync(user, cancellationToken);

            _logger.LogInformation("User '{Email}' created by '{CallerEmail}'.", user.Email, callerEmail);

            await _auditService.LogActionAsync(
                AuditAction.UserCreated,
                callerUserId, callerEmail, callerRole,
                "User", user.Id,
                null,
                JsonSerializer.Serialize(new { user.Id, user.Email, user.Role, user.CustomerId }, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);

            _ = _notificationService.NotifyAsync(
                "User Created",
                $"New user {user.Email} ({user.Role}) was created.",
                new Dictionary<string, string> { ["type"] = "user_created", ["userId"] = user.UserId, ["email"] = user.Email },
                cancellationToken: CancellationToken.None);

            return MapToDto(user);
        }

        /// <inheritdoc/>
        public async Task<UserDto> UpdateUserAsync(
            string userId,
            UpdateUserRequest request,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId is required.", nameof(userId));
            if (request == null) throw new ArgumentNullException(nameof(request));

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException($"User '{userId}' not found.");

            var oldSnapshot = JsonSerializer.Serialize(new { user.Name, user.ProjectScope }, _jsonOptions);

            if (!string.IsNullOrWhiteSpace(request.Name))
                user.Name = request.Name.Trim();

            if (!string.IsNullOrWhiteSpace(request.Email))
                user.Email = request.Email.Trim().ToLowerInvariant();

            if (request.ProjectScope != null)
                user.ProjectScope = request.ProjectScope;

            if (request.IsActive.HasValue)
                user.IsActive = request.IsActive.Value;

            if (!string.IsNullOrWhiteSpace(request.CustomerId))
                user.CustomerId = request.CustomerId.Trim();

            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(userId, user, cancellationToken);

            _logger.LogInformation("User '{UserId}' updated by '{CallerEmail}'.", userId, callerEmail);

            await _auditService.LogActionAsync(
                AuditAction.UserUpdated,
                callerUserId, callerEmail, UserRole.SuperAdmin,
                "User", userId,
                oldSnapshot,
                JsonSerializer.Serialize(new { user.Name, user.ProjectScope }, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);

            _ = _notificationService.NotifyAsync(
                "User Updated",
                $"User {user.Email} was updated.",
                new Dictionary<string, string> { ["type"] = "user_updated", ["userId"] = userId, ["email"] = user.Email },
                cancellationToken: CancellationToken.None);

            return MapToDto(user);
        }

        /// <inheritdoc/>
        public async Task DeleteUserAsync(
            string userId,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId is required.", nameof(userId));

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException($"User '{userId}' not found.");

            if (userId == callerUserId)
                throw new InvalidOperationException("You cannot delete your own account.");

            await _userRepository.DeleteAsync(userId, cancellationToken);

            _logger.LogInformation("User '{UserId}' permanently deleted by '{CallerEmail}'.", userId, callerEmail);

            await _auditService.LogActionAsync(
                AuditAction.UserDeleted,
                callerUserId, callerEmail, UserRole.SuperAdmin,
                "User", userId,
                JsonSerializer.Serialize(new { user.Email, user.Role }, _jsonOptions),
                null,
                ipAddress,
                cancellationToken: cancellationToken);

            _ = _notificationService.NotifyAsync(
                "User Deleted",
                $"User {user.Email} ({user.Role}) was permanently deleted.",
                new Dictionary<string, string> { ["type"] = "user_deleted", ["userId"] = userId, ["email"] = user.Email },
                cancellationToken: CancellationToken.None);
        }

        /// <inheritdoc/>
        public async Task DeactivateUserAsync(
            string userId,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId is required.", nameof(userId));

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException($"User '{userId}' not found.");

            if (!user.IsActive)
                throw new InvalidOperationException("User is already deactivated.");

            await _userRepository.DeactivateAsync(userId, cancellationToken);

            _logger.LogInformation("User '{UserId}' deactivated by '{CallerEmail}'.", userId, callerEmail);

            await _auditService.LogActionAsync(
                AuditAction.UserDeactivated,
                callerUserId, callerEmail, UserRole.SuperAdmin,
                "User", userId,
                JsonSerializer.Serialize(new { user.IsActive }, _jsonOptions),
                JsonSerializer.Serialize(new { IsActive = false }, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);

            _ = _notificationService.NotifyAsync(
                "User Deactivated",
                $"User {user.Email} was deactivated.",
                new Dictionary<string, string> { ["type"] = "user_deactivated", ["userId"] = userId, ["email"] = user.Email },
                cancellationToken: CancellationToken.None);
        }

        /// <inheritdoc/>
        public async Task ActivateUserAsync(
            string userId,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId is required.", nameof(userId));

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException($"User '{userId}' not found.");

            if (user.IsActive)
                throw new InvalidOperationException("User is already active.");

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(userId, user, cancellationToken);

            _logger.LogInformation("User '{UserId}' activated by '{CallerEmail}'.", userId, callerEmail);

            await _auditService.LogActionAsync(
                AuditAction.UserActivated,
                callerUserId, callerEmail, UserRole.SuperAdmin,
                "User", userId,
                JsonSerializer.Serialize(new { IsActive = false }, _jsonOptions),
                JsonSerializer.Serialize(new { IsActive = true }, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);

            _ = _notificationService.NotifyAsync(
                "User Activated",
                $"User {user.Email} was activated.",
                new Dictionary<string, string> { ["type"] = "user_activated", ["userId"] = userId, ["email"] = user.Email },
                cancellationToken: CancellationToken.None);
        }

        /// <inheritdoc/>
        public async Task<UserDto?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId is required.", nameof(userId));

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            return user == null ? null : MapToDto(user);
        }

        /// <inheritdoc/>
        public async Task<PagedResult<UserDto>> GetUsersAsync(string filter, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var users = await _userRepository.SearchUsersAsync(filter, page, pageSize, cancellationToken);
            var total = await _userRepository.CountAsync(filter, cancellationToken);

            return new PagedResult<UserDto>
            {
                Items = users.Select(MapToDto).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        /// <inheritdoc/>
        public async Task AssignRoleAsync(
            string userId,
            UserRole newRole,
            string callerUserId,
            string callerEmail,
            UserRole callerRole,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId is required.", nameof(userId));

            // Only SuperAdmin can assign the SuperAdmin role
            if (newRole == UserRole.SuperAdmin && callerRole != UserRole.SuperAdmin)
                throw new UnauthorizedAccessException("Only a SuperAdmin can assign the SuperAdmin role.");

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException($"User '{userId}' not found.");

            var oldRole = user.Role;
            if (oldRole == newRole)
                return;

            user.Role = newRole;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(userId, user, cancellationToken);

            _logger.LogInformation("Role of user '{UserId}' changed from '{OldRole}' to '{NewRole}' by '{CallerEmail}'.",
                userId, oldRole, newRole, callerEmail);

            await _auditService.LogActionAsync(
                AuditAction.UserRoleChanged,
                callerUserId, callerEmail, callerRole,
                "User", userId,
                JsonSerializer.Serialize(new { Role = oldRole.ToString() }, _jsonOptions),
                JsonSerializer.Serialize(new { Role = newRole.ToString() }, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);

            _ = _notificationService.NotifyAsync(
                "User Role Changed",
                $"User {user.Email} role changed from {oldRole} to {newRole}.",
                new Dictionary<string, string> { ["type"] = "user_role_changed", ["userId"] = userId, ["oldRole"] = oldRole.ToString(), ["newRole"] = newRole.ToString() },
                cancellationToken: CancellationToken.None);
        }

        /// <inheritdoc/>
        public async Task ChangePasswordAsync(
            string userId,
            ChangePasswordRequest request,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId is required.", nameof(userId));
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.CurrentPassword)) throw new ArgumentException("CurrentPassword is required.");
            if (string.IsNullOrWhiteSpace(request.NewPassword)) throw new ArgumentException("NewPassword is required.");
            if (request.NewPassword.Length < 6)
                throw new ArgumentException("New password must be at least 6 characters long.");

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException($"User '{userId}' not found.");

            if (!_authService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
                throw new UnauthorizedAccessException("Current password is incorrect.");

            var newHash = _authService.HashPassword(request.NewPassword);
            await _userRepository.UpdatePasswordAsync(userId, newHash, cancellationToken);

            _logger.LogInformation("Password changed for user '{UserId}'.", userId);

            await _auditService.LogActionAsync(
                AuditAction.PasswordChanged,
                userId, user.Email, user.Role,
                "User", userId,
                null, null,
                ipAddress,
                cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<List<UserDto>> GetUsersByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
        {
            var users = await _userRepository.GetByRoleAsync(role, cancellationToken);
            return users.Select(MapToDto).ToList();
        }

        /// <inheritdoc/>
        public async Task<UserDto> AssignProjectsAsync(
            string userId,
            List<string> projectIds,
            string callerUserId,
            string callerEmail,
            UserRole callerRole,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId is required.", nameof(userId));
            if (projectIds == null) throw new ArgumentNullException(nameof(projectIds));

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException($"User '{userId}' not found.");

            var oldScope = JsonSerializer.Serialize(user.ProjectScope, _jsonOptions);

            user.ProjectScope = projectIds.Distinct().ToList();
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(userId, user, cancellationToken);

            _logger.LogInformation(
                "Project scope of user '{UserId}' updated to [{Projects}] by '{CallerEmail}'.",
                userId, string.Join(", ", user.ProjectScope), callerEmail);

            await _auditService.LogActionAsync(
                AuditAction.UserUpdated,
                callerUserId, callerEmail, callerRole,
                "User", userId,
                oldScope,
                JsonSerializer.Serialize(user.ProjectScope, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);

            return MapToDto(user);
        }

        // ── Private mapper ──────────────────────────────────────────────────────────

        private static UserDto MapToDto(UserEntity u) => new UserDto
        {
            Id = u.Id,
            UserId = u.UserId,
            Name = u.Name,
            Email = u.Email,
            Role = u.Role.ToString(),
            CustomerId = u.CustomerId,
            IsActive = u.IsActive,
            ProjectScope = u.ProjectScope,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt,
            LastLoginAt = u.LastLoginAt
        };
    }
}
