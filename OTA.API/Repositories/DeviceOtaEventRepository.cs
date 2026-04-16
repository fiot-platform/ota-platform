using MongoDB.Driver;
using OTA.API.Models.Entities;
using OTA.API.Repositories.Interfaces;

namespace OTA.API.Repositories
{
    /// <summary>
    /// MongoDB implementation of <see cref="IDeviceOtaEventRepository"/>.
    /// Collection: DeviceOtaEvents
    /// </summary>
    public sealed class DeviceOtaEventRepository : IDeviceOtaEventRepository
    {
        private readonly IMongoCollection<DeviceOtaEventEntity> _collection;

        public DeviceOtaEventRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<DeviceOtaEventEntity>("DeviceOtaEvents");

            // Index on deviceId for per-device queries
            _collection.Indexes.CreateOne(new CreateIndexModel<DeviceOtaEventEntity>(
                Builders<DeviceOtaEventEntity>.IndexKeys.Ascending(e => e.DeviceId),
                new CreateIndexOptions { Name = "idx_otaevents_deviceId" }));
        }

        public async Task LogAsync(DeviceOtaEventEntity evt, CancellationToken cancellationToken = default)
        {
            await _collection.InsertOneAsync(evt, null, cancellationToken);
        }

        public async Task<List<DeviceOtaEventEntity>> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            var filter = Builders<DeviceOtaEventEntity>.Filter.Eq(e => e.DeviceId, deviceId);
            return await _collection.Find(filter)
                .SortByDescending(e => e.Timestamp)
                .ToListAsync(cancellationToken);
        }
    }
}
