using System;
using System.Collections.Generic;
using System.Linq;
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
    /// MongoDB implementation of <see cref="IFirmwareRepository"/>.
    /// Creates indexes on RepositoryId, Status, Channel, and a compound index on (Model, Channel, Status).
    /// </summary>
    public class FirmwareRepository : BaseRepository<FirmwareVersionEntity>, IFirmwareRepository
    {
        /// <summary>
        /// Initialises a new instance of <see cref="FirmwareRepository"/> and ensures required indexes exist.
        /// </summary>
        /// <param name="database">The MongoDB database instance.</param>
        public FirmwareRepository(IMongoDatabase database) : base(database, "Firmwares")
        {
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var indexModels = new List<CreateIndexModel<FirmwareVersionEntity>>
            {
                new CreateIndexModel<FirmwareVersionEntity>(
                    Builders<FirmwareVersionEntity>.IndexKeys.Ascending(f => f.RepositoryId),
                    new CreateIndexOptions { Name = "idx_firmware_repositoryId" }),

                new CreateIndexModel<FirmwareVersionEntity>(
                    Builders<FirmwareVersionEntity>.IndexKeys.Ascending(f => f.ProjectId),
                    new CreateIndexOptions { Name = "idx_firmware_projectId" }),

                new CreateIndexModel<FirmwareVersionEntity>(
                    Builders<FirmwareVersionEntity>.IndexKeys.Ascending(f => f.Status),
                    new CreateIndexOptions { Name = "idx_firmware_status" }),

                new CreateIndexModel<FirmwareVersionEntity>(
                    Builders<FirmwareVersionEntity>.IndexKeys.Ascending(f => f.Channel),
                    new CreateIndexOptions { Name = "idx_firmware_channel" }),

                // Compound index for device update queries
                new CreateIndexModel<FirmwareVersionEntity>(
                    Builders<FirmwareVersionEntity>.IndexKeys
                        .Ascending(f => f.Status)
                        .Ascending(f => f.Channel),
                    new CreateIndexOptions { Name = "idx_firmware_status_channel" })
            };

            Collection.Indexes.CreateMany(indexModels);
        }

        /// <inheritdoc/>
        public async Task<FirmwareVersionEntity?> GetByFirmwareIdAsync(string firmwareId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(firmwareId))
                throw new ArgumentException("FirmwareId must not be null or empty.", nameof(firmwareId));

            var filter = Builders<FirmwareVersionEntity>.Filter.Eq(f => f.FirmwareId, firmwareId);
            return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<List<FirmwareVersionEntity>> GetByRepositoryIdAsync(string repositoryId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(repositoryId))
                throw new ArgumentException("RepositoryId must not be null or empty.", nameof(repositoryId));

            try
            {
                var filter = Builders<FirmwareVersionEntity>.Filter.Eq(f => f.RepositoryId, repositoryId);
                return await Collection.Find(filter)
                    .SortByDescending(f => f.CreatedAt)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve firmware for repository '{repositoryId}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<FirmwareVersionEntity>> GetByProjectIdAsync(string projectId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(projectId))
                throw new ArgumentException("ProjectId must not be null or empty.", nameof(projectId));

            try
            {
                var filter = Builders<FirmwareVersionEntity>.Filter.Eq(f => f.ProjectId, projectId);
                return await Collection.Find(filter)
                    .SortByDescending(f => f.CreatedAt)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve firmware for project '{projectId}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<FirmwareVersionEntity>> GetByStatusAsync(FirmwareStatus status, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<FirmwareVersionEntity>.Filter.Eq(f => f.Status, status);
                return await Collection.Find(filter)
                    .SortByDescending(f => f.CreatedAt)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve firmware with status '{status}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<FirmwareVersionEntity>> GetByChannelAsync(FirmwareChannel channel, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<FirmwareVersionEntity>.Filter.Eq(f => f.Channel, channel);
                return await Collection.Find(filter)
                    .SortByDescending(f => f.CreatedAt)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve firmware for channel '{channel}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<FirmwareVersionEntity>> GetApprovedForModelAsync(string model, string hardwareRevision, FirmwareChannel channel, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(model))
                throw new ArgumentException("Model must not be null or empty.", nameof(model));
            if (string.IsNullOrWhiteSpace(hardwareRevision))
                throw new ArgumentException("HardwareRevision must not be null or empty.", nameof(hardwareRevision));

            try
            {
                var filter = Builders<FirmwareVersionEntity>.Filter.And(
                    Builders<FirmwareVersionEntity>.Filter.Eq(f => f.Status, FirmwareStatus.Approved),
                    Builders<FirmwareVersionEntity>.Filter.Eq(f => f.Channel, channel),
                    Builders<FirmwareVersionEntity>.Filter.AnyEq(f => f.SupportedModels, model),
                    Builders<FirmwareVersionEntity>.Filter.AnyEq(f => f.SupportedHardwareRevisions, hardwareRevision)
                );

                return await Collection.Find(filter)
                    .SortByDescending(f => f.CreatedAt)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve approved firmware for model '{model}', revision '{hardwareRevision}', channel '{channel}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<FirmwareVersionEntity>> SearchAsync(string filter, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            try
            {
                var skip = (page - 1) * pageSize;
                FilterDefinition<FirmwareVersionEntity> mongoFilter;

                if (string.IsNullOrWhiteSpace(filter))
                {
                    mongoFilter = Builders<FirmwareVersionEntity>.Filter.Empty;
                }
                else
                {
                    var regex = new MongoDB.Bson.BsonRegularExpression(filter, "i");
                    mongoFilter = Builders<FirmwareVersionEntity>.Filter.Or(
                        Builders<FirmwareVersionEntity>.Filter.Regex(f => f.Version, regex),
                        Builders<FirmwareVersionEntity>.Filter.Regex(f => f.GiteaTagName, regex),
                        Builders<FirmwareVersionEntity>.Filter.Regex(f => f.ReleaseNotes, regex)
                    );
                }

                return await Collection.Find(mongoFilter)
                    .SortByDescending(f => f.CreatedAt)
                    .Skip(skip)
                    .Limit(pageSize)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to search firmware with filter '{filter}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<(List<FirmwareVersionEntity> Items, long TotalCount)> SearchWithFiltersAsync(
            string? search,
            string? status,
            string? channel,
            string? projectId,
            string? repositoryId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var filters = new List<FilterDefinition<FirmwareVersionEntity>>();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var regex = new MongoDB.Bson.BsonRegularExpression(search, "i");
                filters.Add(Builders<FirmwareVersionEntity>.Filter.Or(
                    Builders<FirmwareVersionEntity>.Filter.Regex(f => f.Version, regex),
                    Builders<FirmwareVersionEntity>.Filter.Regex(f => f.GiteaTagName, regex),
                    Builders<FirmwareVersionEntity>.Filter.Regex(f => f.ReleaseNotes, regex)
                ));
            }

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<FirmwareStatus>(status, true, out var parsedStatus))
            {
                filters.Add(Builders<FirmwareVersionEntity>.Filter.Eq(f => f.Status, parsedStatus));
            }

            if (!string.IsNullOrWhiteSpace(channel) &&
                Enum.TryParse<FirmwareChannel>(channel, true, out var parsedChannel))
            {
                filters.Add(Builders<FirmwareVersionEntity>.Filter.Eq(f => f.Channel, parsedChannel));
            }

            if (!string.IsNullOrWhiteSpace(projectId))
                filters.Add(Builders<FirmwareVersionEntity>.Filter.Eq(f => f.ProjectId, projectId));

            if (!string.IsNullOrWhiteSpace(repositoryId))
                filters.Add(Builders<FirmwareVersionEntity>.Filter.Eq(f => f.RepositoryId, repositoryId));

            var mongoFilter = filters.Count > 0
                ? Builders<FirmwareVersionEntity>.Filter.And(filters)
                : Builders<FirmwareVersionEntity>.Filter.Empty;

            var skip = (page - 1) * pageSize;
            var totalCount = await Collection.CountDocumentsAsync(mongoFilter, null, cancellationToken);
            var items = await Collection.Find(mongoFilter)
                .SortByDescending(f => f.CreatedAt)
                .Skip(skip)
                .Limit(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        /// <inheritdoc/>
        public async Task<Dictionary<FirmwareStatus, long>> CountByStatusAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var pipeline = new[]
                {
                    new BsonDocument("$group", new BsonDocument
                    {
                        { "_id", "$Status" },
                        { "count", new BsonDocument("$sum", 1) }
                    })
                };

                var results = await Collection.Aggregate<BsonDocument>(pipeline, null, cancellationToken).ToListAsync(cancellationToken);

                var dict = new Dictionary<FirmwareStatus, long>();
                foreach (var doc in results)
                {
                    if (Enum.TryParse<FirmwareStatus>(doc["_id"].AsString, out var status))
                    {
                        dict[status] = doc["count"].AsInt64;
                    }
                }

                // Ensure all statuses are represented
                foreach (FirmwareStatus status in Enum.GetValues(typeof(FirmwareStatus)))
                {
                    if (!dict.ContainsKey(status))
                        dict[status] = 0;
                }

                return dict;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to count firmware by status.", ex);
            }
        }
    }
}
