using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using OTA.API.Models.Entities;
using OTA.API.Models.Enums;
using OTA.API.Repositories.Interfaces;

namespace OTA.API.Repositories
{
    /// <summary>
    /// MongoDB implementation of <see cref="IDeviceRepository"/>.
    /// Creates a unique index on SerialNumber plus indexes on CustomerId, SiteId, and Status.
    /// </summary>
    public class DeviceRepository : BaseRepository<DeviceEntity>, IDeviceRepository
    {
        /// <summary>
        /// Initialises a new instance of <see cref="DeviceRepository"/> and ensures required indexes exist.
        /// </summary>
        /// <param name="database">The MongoDB database instance.</param>
        public DeviceRepository(IMongoDatabase database) : base(database, "Devices")
        {
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var indexModels = new List<CreateIndexModel<DeviceEntity>>
            {
                new CreateIndexModel<DeviceEntity>(
                    Builders<DeviceEntity>.IndexKeys.Ascending(d => d.SerialNumber),
                    new CreateIndexOptions { Unique = true, Name = "idx_devices_serialNumber_unique" }),

                new CreateIndexModel<DeviceEntity>(
                    Builders<DeviceEntity>.IndexKeys.Ascending(d => d.CustomerId),
                    new CreateIndexOptions { Name = "idx_devices_customerId" }),

                new CreateIndexModel<DeviceEntity>(
                    Builders<DeviceEntity>.IndexKeys.Ascending(d => d.SiteId),
                    new CreateIndexOptions { Name = "idx_devices_siteId" }),

                new CreateIndexModel<DeviceEntity>(
                    Builders<DeviceEntity>.IndexKeys.Ascending(d => d.Status),
                    new CreateIndexOptions { Name = "idx_devices_status" }),

                new CreateIndexModel<DeviceEntity>(
                    Builders<DeviceEntity>.IndexKeys.Ascending(d => d.Model),
                    new CreateIndexOptions { Name = "idx_devices_model" })
            };

            Collection.Indexes.CreateMany(indexModels);
        }

        /// <inheritdoc/>
        public async Task<DeviceEntity?> GetBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
                throw new ArgumentException("SerialNumber must not be null or empty.", nameof(serialNumber));

            try
            {
                var filter = Builders<DeviceEntity>.Filter.Eq(d => d.SerialNumber, serialNumber);
                return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve device with serial number '{serialNumber}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<DeviceEntity>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(customerId))
                throw new ArgumentException("CustomerId must not be null or empty.", nameof(customerId));

            try
            {
                var filter = Builders<DeviceEntity>.Filter.Eq(d => d.CustomerId, customerId);
                return await Collection.Find(filter).SortBy(d => d.SerialNumber).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve devices for customer '{customerId}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<DeviceEntity>> GetBySiteIdAsync(string siteId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(siteId))
                throw new ArgumentException("SiteId must not be null or empty.", nameof(siteId));

            try
            {
                var filter = Builders<DeviceEntity>.Filter.Eq(d => d.SiteId, siteId);
                return await Collection.Find(filter).SortBy(d => d.SerialNumber).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve devices for site '{siteId}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<DeviceEntity>> GetByStatusAsync(DeviceStatus status, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<DeviceEntity>.Filter.Eq(d => d.Status, status);
                return await Collection.Find(filter).SortBy(d => d.SerialNumber).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve devices with status '{status}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<DeviceEntity>> SearchAsync(string filter, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            try
            {
                var skip = (page - 1) * pageSize;
                FilterDefinition<DeviceEntity> mongoFilter;

                if (string.IsNullOrWhiteSpace(filter))
                {
                    mongoFilter = Builders<DeviceEntity>.Filter.Empty;
                }
                else
                {
                    var regex = new MongoDB.Bson.BsonRegularExpression(filter, "i");
                    mongoFilter = Builders<DeviceEntity>.Filter.Or(
                        Builders<DeviceEntity>.Filter.Regex(d => d.SerialNumber, regex),
                        Builders<DeviceEntity>.Filter.Regex(d => d.Model, regex),
                        Builders<DeviceEntity>.Filter.Regex(d => d.SiteId, regex),
                        Builders<DeviceEntity>.Filter.Regex(d => d.CustomerId, regex)
                    );
                }

                return await Collection.Find(mongoFilter)
                    .SortBy(d => d.SerialNumber)
                    .Skip(skip)
                    .Limit(pageSize)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to search devices with filter '{filter}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<long> CountAsync(string filter, CancellationToken cancellationToken = default)
        {
            try
            {
                FilterDefinition<DeviceEntity> mongoFilter;

                if (string.IsNullOrWhiteSpace(filter))
                {
                    mongoFilter = Builders<DeviceEntity>.Filter.Empty;
                }
                else
                {
                    var regex = new MongoDB.Bson.BsonRegularExpression(filter, "i");
                    mongoFilter = Builders<DeviceEntity>.Filter.Or(
                        Builders<DeviceEntity>.Filter.Regex(d => d.SerialNumber, regex),
                        Builders<DeviceEntity>.Filter.Regex(d => d.Model, regex),
                        Builders<DeviceEntity>.Filter.Regex(d => d.SiteId, regex),
                        Builders<DeviceEntity>.Filter.Regex(d => d.CustomerId, regex)
                    );
                }

                return await Collection.CountDocumentsAsync(mongoFilter, null, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to count devices with filter '{filter}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task UpdateHeartbeatAsync(string deviceId, DateTime lastSeen, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                throw new ArgumentException("DeviceId must not be null or empty.", nameof(deviceId));

            try
            {
                var filter = Builders<DeviceEntity>.Filter.Eq("_id", ObjectId.Parse(deviceId));
                var update = Builders<DeviceEntity>.Update
                    .Set(d => d.LastSeen, lastSeen)
                    .Set(d => d.UpdatedAt, DateTime.UtcNow);

                var result = await Collection.UpdateOneAsync(filter, update, null, cancellationToken);
                if (result.MatchedCount == 0)
                    throw new KeyNotFoundException($"No device found with id '{deviceId}'.");
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to update heartbeat for device '{deviceId}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task UpdateFirmwareVersionAsync(string deviceId, string firmwareVersion, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                throw new ArgumentException("DeviceId must not be null or empty.", nameof(deviceId));
            if (string.IsNullOrWhiteSpace(firmwareVersion))
                throw new ArgumentException("FirmwareVersion must not be null or empty.", nameof(firmwareVersion));

            try
            {
                var filter = Builders<DeviceEntity>.Filter.Eq("_id", ObjectId.Parse(deviceId));
                var update = Builders<DeviceEntity>.Update
                    .Set(d => d.CurrentFirmwareVersion, firmwareVersion)
                    .Set(d => d.UpdatedAt, DateTime.UtcNow);

                var result = await Collection.UpdateOneAsync(filter, update, null, cancellationToken);
                if (result.MatchedCount == 0)
                    throw new KeyNotFoundException($"No device found with id '{deviceId}'.");
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to update firmware version for device '{deviceId}'.", ex);
            }
        }
    }
}
