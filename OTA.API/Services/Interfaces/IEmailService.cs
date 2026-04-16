namespace OTA.API.Services.Interfaces
{
    /// <summary>
    /// Provides methods for sending transactional emails from the OTA Platform.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>Sends a plain-text or HTML email to a single recipient.</summary>
        Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken cancellationToken = default);

        /// <summary>Sends a firmware approval notification to the specified approver email.</summary>
        Task SendFirmwareApprovalNotificationAsync(string toEmail, string approverName, string firmwareVersion, string projectName, CancellationToken cancellationToken = default);

        /// <summary>Sends a firmware rejection notification to the submitter.</summary>
        Task SendFirmwareRejectionNotificationAsync(string toEmail, string submitterName, string firmwareVersion, string reason, CancellationToken cancellationToken = default);

        /// <summary>Sends an OTA rollout completion summary email.</summary>
        Task SendRolloutCompletionNotificationAsync(string toEmail, string recipientName, string rolloutName, int totalDevices, int succeeded, int failed, CancellationToken cancellationToken = default);

        /// <summary>Sends a welcome / account-created email to a new user.</summary>
        Task SendWelcomeEmailAsync(string toEmail, string userName, string temporaryPassword, CancellationToken cancellationToken = default);
    }
}
