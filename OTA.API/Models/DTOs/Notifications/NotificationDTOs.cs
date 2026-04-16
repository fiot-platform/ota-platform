namespace OTA.API.Models.DTOs.Notifications
{
    // ─────────────────────────────────────────────────────────────────────────
    // Requests
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Request to register or refresh a Firebase Cloud Messaging device token for the
    /// authenticated user. Tokens are stored per-user and used to push notifications.
    /// </summary>
    public sealed class RegisterFcmTokenRequest
    {
        /// <summary>
        /// FCM registration token obtained from the Firebase SDK on the client device or browser.
        /// Must not be null or empty.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Optional human-readable label for the device/browser (e.g., "Chrome on MacBook", "Android Phone").
        /// Helps users identify which devices receive notifications.
        /// </summary>
        public string? DeviceLabel { get; set; }
    }

    /// <summary>
    /// Request to remove a previously registered FCM token for the authenticated user.
    /// Called when the user logs out from a device.
    /// </summary>
    public sealed class UnregisterFcmTokenRequest
    {
        /// <summary>The FCM token to remove.</summary>
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request to send a custom ad-hoc push notification. Restricted to SuperAdmin / PlatformAdmin.
    /// </summary>
    public sealed class SendNotificationRequest
    {
        /// <summary>Notification title shown in the push banner.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Notification body text.</summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Optional data payload sent alongside the notification (key/value pairs).
        /// Clients can use these values to navigate to the correct screen.
        /// </summary>
        public Dictionary<string, string>? Data { get; set; }

        /// <summary>
        /// Target FCM tokens to receive this notification.
        /// Send to specific users' registered tokens. Mutually exclusive with <see cref="Topic"/>.
        /// </summary>
        public List<string>? Tokens { get; set; }

        /// <summary>
        /// FCM topic to publish to (e.g., "rollouts", "firmware-approvals").
        /// All clients subscribed to this topic will receive the message.
        /// Mutually exclusive with <see cref="Tokens"/>.
        /// </summary>
        public string? Topic { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Responses / View models
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Summary of a registered FCM token entry stored for a user.
    /// </summary>
    public sealed class FcmTokenDto
    {
        /// <summary>The FCM registration token string.</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>Human-readable label provided at registration time.</summary>
        public string? DeviceLabel { get; set; }

        /// <summary>UTC timestamp when this token was registered or last refreshed.</summary>
        public DateTime RegisteredAt { get; set; }
    }

    /// <summary>
    /// Result returned after attempting to send a push notification.
    /// </summary>
    public sealed class NotificationResultDto
    {
        /// <summary>True if the FCM send succeeded for at least one target.</summary>
        public bool Success { get; set; }

        /// <summary>Human-readable description of the outcome.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Number of tokens the message was successfully dispatched to.</summary>
        public int SuccessCount { get; set; }

        /// <summary>Number of tokens that failed (invalid / expired).</summary>
        public int FailureCount { get; set; }

        /// <summary>Tokens that were reported as invalid by FCM and have been removed.</summary>
        public List<string> InvalidTokensRemoved { get; set; } = new();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Notification Inbox
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A single notification in the user's in-app inbox.
    /// </summary>
    public sealed class InboxNotificationDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public Dictionary<string, string>? Data { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Paginated inbox response for the current user.
    /// </summary>
    public sealed class InboxResponse
    {
        public List<InboxNotificationDto> Notifications { get; set; } = new();
        public int UnreadCount { get; set; }
    }
}
