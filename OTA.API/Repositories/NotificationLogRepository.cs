using MongoDB.Bson;
using MongoDB.Driver;
using OTA.API.Models.Entities;
using OTA.API.Repositories.Interfaces;

namespace OTA.API.Repositories
{
    /// <summary>
    /// MongoDB implementation of <see cref="INotificationLogRepository"/>.
    /// Stores notification records in the <c>NotificationLogs</c> collection.
    /// </summary>
    public sealed class NotificationLogRepository : INotificationLogRepository
    {
        private readonly IMongoCollection<NotificationLogEntity> _collection;

        public NotificationLogRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<NotificationLogEntity>("NotificationLogs");
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var models = new List<CreateIndexModel<NotificationLogEntity>>
            {
                new(Builders<NotificationLogEntity>.IndexKeys.Descending(n => n.CreatedAt),
                    new CreateIndexOptions { Name = "idx_notif_createdAt_desc" }),

                new(Builders<NotificationLogEntity>.IndexKeys.Ascending(n => n.TargetRoles),
                    new CreateIndexOptions { Name = "idx_notif_targetRoles" }),
            };
            _collection.Indexes.CreateMany(models);
        }

        public async Task InsertAsync(NotificationLogEntity entity, CancellationToken cancellationToken = default)
        {
            await _collection.InsertOneAsync(entity, null, cancellationToken);
        }

        public async Task<List<NotificationLogEntity>> GetForRoleAsync(string role, int limit = 50, CancellationToken cancellationToken = default)
        {
            var filter = Builders<NotificationLogEntity>.Filter.AnyEq(n => n.TargetRoles, role);
            return await _collection
                .Find(filter)
                .SortByDescending(n => n.CreatedAt)
                .Limit(limit)
                .ToListAsync(cancellationToken);
        }

        public async Task MarkAsReadAsync(string notificationId, string userId, CancellationToken cancellationToken = default)
        {
            if (!ObjectId.TryParse(notificationId, out _)) return;

            var filter = Builders<NotificationLogEntity>.Filter.And(
                Builders<NotificationLogEntity>.Filter.Eq("_id", ObjectId.Parse(notificationId)),
                Builders<NotificationLogEntity>.Filter.Not(
                    Builders<NotificationLogEntity>.Filter.AnyEq(n => n.ReadByUserIds, userId))
            );
            var update = Builders<NotificationLogEntity>.Update.Push(n => n.ReadByUserIds, userId);
            await _collection.UpdateOneAsync(filter, update, null, cancellationToken);
        }

        public async Task MarkAllAsReadAsync(string role, string userId, CancellationToken cancellationToken = default)
        {
            var filter = Builders<NotificationLogEntity>.Filter.And(
                Builders<NotificationLogEntity>.Filter.AnyEq(n => n.TargetRoles, role),
                Builders<NotificationLogEntity>.Filter.Not(
                    Builders<NotificationLogEntity>.Filter.AnyEq(n => n.ReadByUserIds, userId))
            );
            var update = Builders<NotificationLogEntity>.Update.Push(n => n.ReadByUserIds, userId);
            await _collection.UpdateManyAsync(filter, update, null, cancellationToken);
        }
    }
}
