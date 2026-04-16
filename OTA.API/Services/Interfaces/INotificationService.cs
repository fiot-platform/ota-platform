using OTA.API.Models.DTOs.Notifications;
using OTA.API.Models.Enums;

namespace OTA.API.Services.Interfaces
{
    /// <summary>
    /// Abstraction over Firebase Cloud Messaging (FCM) for sending push notifications
    /// to registered user devices and browsers.
    /// </summary>
    public interface INotificationService
    {
        // ── Token management ──────────────────────────────────────────────────

        /// <summary>
        /// Persists an FCM registration token for the given user.
        /// If the token already exists the <paramref name="deviceLabel"/> and timestamp are refreshed.
        /// </summary>
        Task RegisterTokenAsync(string userId, string token, string? deviceLabel, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a previously registered FCM token for the given user.
        /// No-op if the token is not found.
        /// </summary>
        Task UnregisterTokenAsync(string userId, string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns all FCM token entries registered by the given user.
        /// </summary>
        Task<List<FcmTokenDto>> GetUserTokensAsync(string userId, CancellationToken cancellationToken = default);

        // ── Generic event notification ────────────────────────────────────────

        /// <summary>
        /// Sends a push notification to all users whose role is in <paramref name="roles"/>
        /// (defaults to SuperAdmin, PlatformAdmin, and ReleaseManager when null).
        /// Fire-and-forget safe: the implementation swallows FCM errors internally.
        /// </summary>
        Task NotifyAsync(
            string title,
            string body,
            Dictionary<string, string>? data = null,
            UserRole[]? roles = null,
            CancellationToken cancellationToken = default);

        // ── In-app notification inbox ─────────────────────────────────────────

        /// <summary>
        /// Returns the in-app notification inbox for a user of the given role.
        /// </summary>
        Task<InboxResponse> GetInboxAsync(string userId, string role, int limit = 50, CancellationToken cancellationToken = default);

        /// <summary>Marks a single notification as read for the given user.</summary>
        Task MarkAsReadAsync(string notificationId, string userId, CancellationToken cancellationToken = default);

        /// <summary>Marks all notifications in the user's inbox as read.</summary>
        Task MarkAllAsReadAsync(string userId, string role, CancellationToken cancellationToken = default);

        // ── Low-level send primitives ─────────────────────────────────────────

        /// <summary>
        /// Sends a push notification to one or more explicit FCM registration tokens.
        /// Invalid/expired tokens reported by FCM are automatically removed from user records.
        /// </summary>
        Task<NotificationResultDto> SendToTokensAsync(
            IEnumerable<string> tokens,
            string title,
            string body,
            Dictionary<string, string>? data = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes a push notification to an FCM topic.
        /// All client apps subscribed to the topic will receive the message.
        /// </summary>
        Task<NotificationResultDto> SendToTopicAsync(
            string topic,
            string title,
            string body,
            Dictionary<string, string>? data = null,
            CancellationToken cancellationToken = default);

        // ── High-level domain notification helpers ────────────────────────────

        /// <summary>
        /// Notifies SuperAdmin, PlatformAdmin, and ReleaseManager users that a rollout
        /// has been started.
        /// </summary>
        Task NotifyRolloutStartedAsync(string rolloutId, string rolloutName, string projectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Notifies SuperAdmin, PlatformAdmin, and ReleaseManager users that a rollout
        /// has completed with final success/failure counts.
        /// </summary>
        Task NotifyRolloutCompletedAsync(string rolloutId, string rolloutName, int succeeded, int failed, string projectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Notifies SuperAdmin, PlatformAdmin, and ReleaseManager users that a rollout
        /// has failed.
        /// </summary>
        Task NotifyRolloutFailedAsync(string rolloutId, string rolloutName, string reason, string projectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Notifies SuperAdmin, PlatformAdmin, and ReleaseManager users that a firmware
        /// version has been approved and is ready for rollout.
        /// </summary>
        Task NotifyFirmwareApprovedAsync(string firmwareId, string version, CancellationToken cancellationToken = default);

        /// <summary>
        /// Notifies SuperAdmin, PlatformAdmin, and ReleaseManager users that a firmware
        /// version was rejected during the approval workflow.
        /// </summary>
        Task NotifyFirmwareRejectedAsync(string firmwareId, string version, string reason, CancellationToken cancellationToken = default);
    }
}
