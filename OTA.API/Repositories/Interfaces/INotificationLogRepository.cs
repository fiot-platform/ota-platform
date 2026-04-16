using OTA.API.Models.Entities;

namespace OTA.API.Repositories.Interfaces
{
    /// <summary>
    /// Repository for persisting and querying platform notification log entries.
    /// </summary>
    public interface INotificationLogRepository
    {
        /// <summary>Inserts a new notification log record.</summary>
        Task InsertAsync(NotificationLogEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the most recent notifications visible to the given role,
        /// ordered by <see cref="NotificationLogEntity.CreatedAt"/> descending.
        /// </summary>
        Task<List<NotificationLogEntity>> GetForRoleAsync(string role, int limit = 50, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds <paramref name="userId"/> to <see cref="NotificationLogEntity.ReadByUserIds"/>
        /// for the specified notification.
        /// </summary>
        Task MarkAsReadAsync(string notificationId, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks all notifications for the given role as read by the given user.
        /// </summary>
        Task MarkAllAsReadAsync(string role, string userId, CancellationToken cancellationToken = default);
    }
}
