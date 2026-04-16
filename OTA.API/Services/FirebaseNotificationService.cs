using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OTA.API.Models.DTOs.Notifications;
using OTA.API.Models.Entities;
using OTA.API.Models.Enums;
using OTA.API.Models.Settings;
using OTA.API.Repositories.Interfaces;
using OTA.API.Services.Interfaces;

namespace OTA.API.Services
{
    /// <summary>
    /// Firebase Cloud Messaging implementation of <see cref="INotificationService"/>.
    /// Sends push notifications to registered user devices and browsers via FCM.
    /// When <see cref="FirebaseSettings.Enabled"/> is false the service silently no-ops
    /// so the rest of the platform continues to function without a Firebase project.
    /// </summary>
    public class FirebaseNotificationService : INotificationService
    {
        // Roles that receive admin/release broadcast notifications.
        private static readonly UserRole[] AdminRoles =
        {
            UserRole.SuperAdmin,
            UserRole.PlatformAdmin,
            UserRole.ReleaseManager
        };

        private readonly IUserRepository _userRepository;
        private readonly INotificationLogRepository _notifLogRepo;
        private readonly FirebaseSettings _settings;
        private readonly ILogger<FirebaseNotificationService> _logger;

        public FirebaseNotificationService(
            IUserRepository userRepository,
            INotificationLogRepository notifLogRepo,
            IOptions<FirebaseSettings> settings,
            ILogger<FirebaseNotificationService> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _notifLogRepo   = notifLogRepo   ?? throw new ArgumentNullException(nameof(notifLogRepo));
            _settings       = settings?.Value  ?? throw new ArgumentNullException(nameof(settings));
            _logger         = logger            ?? throw new ArgumentNullException(nameof(logger));
        }

        // ── Token management ──────────────────────────────────────────────────

        /// <inheritdoc/>
        public async Task RegisterTokenAsync(string userId, string token, string? deviceLabel, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId is required.", nameof(userId));
            if (string.IsNullOrWhiteSpace(token))  throw new ArgumentException("Token is required.",  nameof(token));

            await _userRepository.AddOrUpdateFcmTokenAsync(userId, token, deviceLabel, cancellationToken);
            _logger.LogInformation("FCM token registered for user {UserId} (label: {Label}).", userId, deviceLabel ?? "—");
        }

        /// <inheritdoc/>
        public async Task UnregisterTokenAsync(string userId, string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId is required.", nameof(userId));
            if (string.IsNullOrWhiteSpace(token))  throw new ArgumentException("Token is required.",  nameof(token));

            await _userRepository.RemoveFcmTokenAsync(userId, token, cancellationToken);
            _logger.LogInformation("FCM token unregistered for user {UserId}.", userId);
        }

        /// <inheritdoc/>
        public async Task<List<FcmTokenDto>> GetUserTokensAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId is required.", nameof(userId));

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null) return new List<FcmTokenDto>();

            return user.FcmTokens.Select(t => new FcmTokenDto
            {
                Token        = t.Token,
                DeviceLabel  = t.DeviceLabel,
                RegisteredAt = t.RegisteredAt
            }).ToList();
        }

        // ── Generic event notification ────────────────────────────────────────

        /// <inheritdoc/>
        public async Task NotifyAsync(
            string title,
            string body,
            Dictionary<string, string>? data = null,
            UserRole[]? roles = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var targetRoles = (roles != null && roles.Length > 0) ? roles : AdminRoles;

                // Persist to inbox so users can see it in the dashboard
                var logEntry = new NotificationLogEntity
                {
                    Title       = title,
                    Body        = body,
                    Data        = data,
                    TargetRoles = targetRoles.Select(r => r.ToString()).ToList(),
                    CreatedAt   = DateTime.UtcNow
                };
                await _notifLogRepo.InsertAsync(logEntry, cancellationToken);

                // Push via FCM if any tokens are registered
                var tokens = await GetTokensForRolesAsync(targetRoles, cancellationToken);
                if (tokens.Any())
                    await SendToTokensAsync(tokens, title, body, data, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "NotifyAsync failed silently. Title={Title}", title);
            }
        }

        // ── In-app inbox ──────────────────────────────────────────────────────

        /// <inheritdoc/>
        public async Task<InboxResponse> GetInboxAsync(string userId, string role, int limit = 50, CancellationToken cancellationToken = default)
        {
            var items = await _notifLogRepo.GetForRoleAsync(role, limit, cancellationToken);
            var dtos  = items.Select(n => new InboxNotificationDto
            {
                Id        = n.Id,
                Title     = n.Title,
                Body      = n.Body,
                Data      = n.Data,
                IsRead    = n.ReadByUserIds.Contains(userId),
                CreatedAt = n.CreatedAt
            }).ToList();

            return new InboxResponse
            {
                Notifications = dtos,
                UnreadCount   = dtos.Count(d => !d.IsRead)
            };
        }

        /// <inheritdoc/>
        public async Task MarkAsReadAsync(string notificationId, string userId, CancellationToken cancellationToken = default)
            => await _notifLogRepo.MarkAsReadAsync(notificationId, userId, cancellationToken);

        /// <inheritdoc/>
        public async Task MarkAllAsReadAsync(string userId, string role, CancellationToken cancellationToken = default)
            => await _notifLogRepo.MarkAllAsReadAsync(role, userId, cancellationToken);

        // ── Low-level send primitives ─────────────────────────────────────────

        /// <inheritdoc/>
        public async Task<NotificationResultDto> SendToTokensAsync(
            IEnumerable<string> tokens,
            string title,
            string body,
            Dictionary<string, string>? data = null,
            CancellationToken cancellationToken = default)
        {
            var tokenList = tokens?.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList()
                            ?? new List<string>();

            var result = new NotificationResultDto { Message = "No tokens provided." };
            if (!tokenList.Any()) return result;

            if (!_settings.Enabled)
            {
                _logger.LogDebug("Firebase notifications disabled. Would have sent to {Count} token(s): {Title}",
                    tokenList.Count, title);
                return new NotificationResultDto
                {
                    Success      = true,
                    Message      = "Firebase disabled — notification skipped.",
                    SuccessCount = 0,
                    FailureCount = 0
                };
            }

            try
            {
                // FCM allows a maximum of 500 tokens per SendEachAsync call.
                const int batchSize = 500;
                int successCount = 0, failureCount = 0;
                var invalidTokens = new List<string>();

                for (int i = 0; i < tokenList.Count; i += batchSize)
                {
                    var batch = tokenList.Skip(i).Take(batchSize).ToList();
                    var messages = batch.Select(token => BuildMessage(token, title, body, data)).ToList();

                    var batchResponse = await FirebaseMessaging.DefaultInstance
                        .SendEachAsync(messages, cancellationToken);

                    for (int j = 0; j < batchResponse.Responses.Count; j++)
                    {
                        var response = batchResponse.Responses[j];
                        if (response.IsSuccess)
                        {
                            successCount++;
                        }
                        else
                        {
                            failureCount++;
                            var errorCode = response.Exception?.MessagingErrorCode;
                            if (errorCode == MessagingErrorCode.Unregistered ||
                                errorCode == MessagingErrorCode.InvalidArgument)
                            {
                                invalidTokens.Add(batch[j]);
                            }
                            _logger.LogWarning("FCM send failed for token {Token}: {Error}",
                                batch[j], response.Exception?.Message);
                        }
                    }
                }

                // Clean up expired/invalid tokens in the background.
                _ = CleanupInvalidTokensAsync(invalidTokens);

                result = new NotificationResultDto
                {
                    Success              = successCount > 0,
                    Message              = $"Sent to {successCount} token(s); {failureCount} failed.",
                    SuccessCount         = successCount,
                    FailureCount         = failureCount,
                    InvalidTokensRemoved = invalidTokens
                };

                _logger.LogInformation("FCM batch: success={S} failure={F} title={Title}",
                    successCount, failureCount, title);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FCM SendToTokensAsync failed. Title={Title}", title);
                return new NotificationResultDto
                {
                    Success      = false,
                    Message      = $"FCM send error: {ex.Message}",
                    FailureCount = tokenList.Count
                };
            }
        }

        /// <inheritdoc/>
        public async Task<NotificationResultDto> SendToTopicAsync(
            string topic,
            string title,
            string body,
            Dictionary<string, string>? data = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(topic)) throw new ArgumentException("Topic is required.", nameof(topic));

            if (!_settings.Enabled)
            {
                _logger.LogDebug("Firebase disabled. Would send to topic {Topic}: {Title}", topic, title);
                return new NotificationResultDto
                {
                    Success = true,
                    Message = "Firebase disabled — notification skipped."
                };
            }

            try
            {
                var message = new Message
                {
                    Topic        = topic,
                    Notification = new Notification { Title = title, Body = body },
                    Data         = data
                };

                var messageId = await FirebaseMessaging.DefaultInstance.SendAsync(message, cancellationToken);
                _logger.LogInformation("FCM topic message sent. Topic={Topic} MessageId={Id}", topic, messageId);

                return new NotificationResultDto
                {
                    Success      = true,
                    Message      = $"Topic message sent (id: {messageId}).",
                    SuccessCount = 1
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FCM SendToTopicAsync failed. Topic={Topic}", topic);
                return new NotificationResultDto
                {
                    Success  = false,
                    Message  = $"FCM topic send error: {ex.Message}",
                    FailureCount = 1
                };
            }
        }

        // ── Domain notification helpers ────────────────────────────────────────

        /// <inheritdoc/>
        public async Task NotifyRolloutStartedAsync(string rolloutId, string rolloutName, string projectId, CancellationToken cancellationToken = default)
        {
            var tokens = await GetAdminTokensAsync(cancellationToken);
            if (!tokens.Any()) return;

            await SendToTokensAsync(tokens,
                title: "Rollout Started",
                body:  $"OTA rollout \"{rolloutName}\" has started.",
                data: new Dictionary<string, string>
                {
                    ["type"]      = "rollout_started",
                    ["rolloutId"] = rolloutId,
                    ["projectId"] = projectId
                },
                cancellationToken);
        }

        /// <inheritdoc/>
        public async Task NotifyRolloutCompletedAsync(string rolloutId, string rolloutName, int succeeded, int failed, string projectId, CancellationToken cancellationToken = default)
        {
            var tokens = await GetAdminTokensAsync(cancellationToken);
            if (!tokens.Any()) return;

            await SendToTokensAsync(tokens,
                title: "Rollout Completed",
                body:  $"Rollout \"{rolloutName}\" finished — {succeeded} succeeded, {failed} failed.",
                data: new Dictionary<string, string>
                {
                    ["type"]      = "rollout_completed",
                    ["rolloutId"] = rolloutId,
                    ["projectId"] = projectId,
                    ["succeeded"] = succeeded.ToString(),
                    ["failed"]    = failed.ToString()
                },
                cancellationToken);
        }

        /// <inheritdoc/>
        public async Task NotifyRolloutFailedAsync(string rolloutId, string rolloutName, string reason, string projectId, CancellationToken cancellationToken = default)
        {
            var tokens = await GetAdminTokensAsync(cancellationToken);
            if (!tokens.Any()) return;

            await SendToTokensAsync(tokens,
                title: "Rollout Failed",
                body:  $"Rollout \"{rolloutName}\" failed: {reason}",
                data: new Dictionary<string, string>
                {
                    ["type"]      = "rollout_failed",
                    ["rolloutId"] = rolloutId,
                    ["projectId"] = projectId,
                    ["reason"]    = reason
                },
                cancellationToken);
        }

        /// <inheritdoc/>
        public async Task NotifyFirmwareApprovedAsync(string firmwareId, string version, CancellationToken cancellationToken = default)
        {
            var tokens = await GetAdminTokensAsync(cancellationToken);
            if (!tokens.Any()) return;

            await SendToTokensAsync(tokens,
                title: "Firmware Approved",
                body:  $"Firmware v{version} has been approved and is ready for rollout.",
                data: new Dictionary<string, string>
                {
                    ["type"]       = "firmware_approved",
                    ["firmwareId"] = firmwareId,
                    ["version"]    = version
                },
                cancellationToken);
        }

        /// <inheritdoc/>
        public async Task NotifyFirmwareRejectedAsync(string firmwareId, string version, string reason, CancellationToken cancellationToken = default)
        {
            var tokens = await GetAdminTokensAsync(cancellationToken);
            if (!tokens.Any()) return;

            await SendToTokensAsync(tokens,
                title: "Firmware Rejected",
                body:  $"Firmware v{version} was rejected: {reason}",
                data: new Dictionary<string, string>
                {
                    ["type"]       = "firmware_rejected",
                    ["firmwareId"] = firmwareId,
                    ["version"]    = version,
                    ["reason"]     = reason
                },
                cancellationToken);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>Queries FCM tokens for all users that have any of the given roles.</summary>
        private async Task<List<string>> GetTokensForRolesAsync(UserRole[] roles, CancellationToken cancellationToken = default)
        {
            try
            {
                var users = await _userRepository.GetByRolesAsync(roles, cancellationToken);
                return users
                    .SelectMany(u => u.FcmTokens.Select(t => t.Token))
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct()
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve FCM tokens for roles.");
                return new List<string>();
            }
        }

        private Task<List<string>> GetAdminTokensAsync(CancellationToken cancellationToken) =>
            GetTokensForRolesAsync(AdminRoles, cancellationToken);

        private static Message BuildMessage(string token, string title, string body, Dictionary<string, string>? data) =>
            new Message
            {
                Token        = token,
                Notification = new Notification { Title = title, Body = body },
                Data         = data
            };

        private async Task CleanupInvalidTokensAsync(List<string> invalidTokens)
        {
            if (!invalidTokens.Any()) return;
            foreach (var token in invalidTokens)
            {
                try
                {
                    await _userRepository.RemoveFcmTokenGloballyAsync(token);
                    _logger.LogInformation("Removed invalid FCM token {Token}.", token);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to remove invalid FCM token {Token}.", token);
                }
            }
        }
    }
}
