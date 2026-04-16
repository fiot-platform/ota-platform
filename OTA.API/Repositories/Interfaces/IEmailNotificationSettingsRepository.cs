using OTA.API.Models.Entities;

namespace OTA.API.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for the singleton EmailNotificationSettings document.
    /// </summary>
    public interface IEmailNotificationSettingsRepository
    {
        /// <summary>Returns the current notification settings, or null if none have been saved yet.</summary>
        Task<EmailNotificationSettingsEntity?> GetAsync(CancellationToken cancellationToken = default);

        /// <summary>Persists (upserts) the notification settings document.</summary>
        Task SaveAsync(EmailNotificationSettingsEntity settings, CancellationToken cancellationToken = default);
    }
}
