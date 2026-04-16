using System.ComponentModel.DataAnnotations;

namespace OTA.API.Models.DTOs
{
    /// <summary>
    /// Read model returned by GET /api/settings/email-notifications.
    /// </summary>
    public sealed class EmailNotificationSettingsDto
    {
        // Firmware events
        public bool OnFirmwareSubmitted  { get; set; }
        public bool OnFirmwareApproved   { get; set; }
        public bool OnFirmwareRejected   { get; set; }
        public bool OnFirmwareQAVerified { get; set; }

        // Rollout events
        public bool OnRolloutStarted   { get; set; }
        public bool OnRolloutCompleted { get; set; }
        public bool OnRolloutFailed    { get; set; }

        // Device events
        public bool OnDeviceOtaFailed  { get; set; }
        public bool OnDeviceRegistered { get; set; }

        // User events
        public bool OnNewUserCreated  { get; set; }
        public bool OnUserDeactivated { get; set; }

        // Recipients
        public List<string> NotifyEmails { get; set; } = new();

        public DateTime? UpdatedAt { get; set; }
        public string?   UpdatedBy { get; set; }
    }

    /// <summary>
    /// Write model accepted by PUT /api/settings/email-notifications.
    /// </summary>
    public sealed class UpdateEmailNotificationSettingsRequest
    {
        // Firmware events
        public bool OnFirmwareSubmitted  { get; set; }
        public bool OnFirmwareApproved   { get; set; }
        public bool OnFirmwareRejected   { get; set; }
        public bool OnFirmwareQAVerified { get; set; }

        // Rollout events
        public bool OnRolloutStarted   { get; set; }
        public bool OnRolloutCompleted { get; set; }
        public bool OnRolloutFailed    { get; set; }

        // Device events
        public bool OnDeviceOtaFailed  { get; set; }
        public bool OnDeviceRegistered { get; set; }

        // User events
        public bool OnNewUserCreated  { get; set; }
        public bool OnUserDeactivated { get; set; }

        // Recipients — each entry must be a valid email address
        public List<string> NotifyEmails { get; set; } = new();
    }
}
