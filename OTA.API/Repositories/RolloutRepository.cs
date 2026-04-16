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
    /// MongoDB implementation of <see cref="IRolloutRepository"/>.
    /// Creates indexes on ProjectId, Status, and FirmwareId.
    /// </summary>
    public class RolloutRepository : BaseRepository<RolloutEntity>, IRolloutRepository
    {
        /// <summary>
        /// Initialises a new instance of <see cref="RolloutRepository"/> and ensures required indexes exist.
        /// </summary>
        /// <param name="database">The MongoDB database instance.</param>
        public RolloutRepository(IMongoDatabase database) : base(database, "Rollouts")
        {
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var indexModels = new List<CreateIndexModel<RolloutEntity>>
            {
                new CreateIndexModel<RolloutEntity>(
                    Builders<RolloutEntity>.IndexKeys.Ascending(r => r.ProjectId),
                    new CreateIndexOptions { Name = "idx_rollouts_projectId" }),

                new CreateIndexModel<RolloutEntity>(
                    Builders<RolloutEntity>.IndexKeys.Ascending(r => r.Status),
                    new CreateIndexOptions { Name = "idx_rollouts_status" }),

                new CreateIndexModel<RolloutEntity>(
                    Builders<RolloutEntity>.IndexKeys.Ascending(r => r.FirmwareId),
                    new CreateIndexOptions { Name = "idx_rollouts_firmwareId" }),

                new CreateIndexModel<RolloutEntity>(
                    Builders<RolloutEntity>.IndexKeys.Descending(r => r.CreatedAt),
                    new CreateIndexOptions { Name = "idx_rollouts_createdAt_desc" })
            };

            Collection.Indexes.CreateMany(indexModels);
        }

        /// <inheritdoc/>
        public async Task<List<RolloutEntity>> GetByProjectIdAsync(string projectId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(projectId))
                throw new ArgumentException("ProjectId must not be null or empty.", nameof(projectId));

            try
            {
                var filter = Builders<RolloutEntity>.Filter.Eq(r => r.ProjectId, projectId);
                return await Collection.Find(filter).SortByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve rollouts for project '{projectId}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<RolloutEntity>> GetByStatusAsync(RolloutStatus status, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<RolloutEntity>.Filter.Eq(r => r.Status, status);
                return await Collection.Find(filter).SortByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve rollouts with status '{status}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<RolloutEntity>> GetByFirmwareIdAsync(string firmwareId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(firmwareId))
                throw new ArgumentException("FirmwareId must not be null or empty.", nameof(firmwareId));

            try
            {
                var filter = Builders<RolloutEntity>.Filter.Eq(r => r.FirmwareId, firmwareId);
                return await Collection.Find(filter).SortByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve rollouts for firmware '{firmwareId}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<RolloutEntity>> SearchAsync(string filter, int page, int pageSize, string? projectId = null, CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            try
            {
                var skip = (page - 1) * pageSize;
                FilterDefinition<RolloutEntity> mongoFilter;

                if (string.IsNullOrWhiteSpace(filter))
                    mongoFilter = Builders<RolloutEntity>.Filter.Empty;
                else
                {
                    var regex = new MongoDB.Bson.BsonRegularExpression(filter, "i");
                    mongoFilter = Builders<RolloutEntity>.Filter.Or(
                        Builders<RolloutEntity>.Filter.Regex(r => r.Name, regex),
                        Builders<RolloutEntity>.Filter.Regex(r => r.Description, regex)
                    );
                }

                if (!string.IsNullOrWhiteSpace(projectId))
                    mongoFilter &= Builders<RolloutEntity>.Filter.Eq(r => r.ProjectId, projectId);

                return await Collection.Find(mongoFilter)
                    .SortByDescending(r => r.CreatedAt)
                    .Skip(skip)
                    .Limit(pageSize)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to search rollouts with filter '{filter}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task UpdateStatusAsync(string rolloutId, RolloutStatus status, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rolloutId))
                throw new ArgumentException("RolloutId must not be null or empty.", nameof(rolloutId));

            try
            {
                var filter = Builders<RolloutEntity>.Filter.Eq("_id", ObjectId.Parse(rolloutId));
                var update = Builders<RolloutEntity>.Update
                    .Set(r => r.Status, status)
                    .Set(r => r.UpdatedAt, DateTime.UtcNow);

                var result = await Collection.UpdateOneAsync(filter, update, null, cancellationToken);
                if (result.MatchedCount == 0)
                    throw new KeyNotFoundException($"No rollout found with id '{rolloutId}'.");
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to update status for rollout '{rolloutId}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task UpdateCountersAsync(string rolloutId, int successDelta, int failureDelta, int pendingDelta, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rolloutId))
                throw new ArgumentException("RolloutId must not be null or empty.", nameof(rolloutId));

            try
            {
                var filter = Builders<RolloutEntity>.Filter.Eq("_id", ObjectId.Parse(rolloutId));
                var updateDef = Builders<RolloutEntity>.Update
                    .Inc(r => r.SuccessCount, successDelta)
                    .Inc(r => r.FailureCount, failureDelta)
                    .Inc(r => r.PendingCount, pendingDelta)
                    .Set(r => r.UpdatedAt, DateTime.UtcNow);

                var result = await Collection.UpdateOneAsync(filter, updateDef, null, cancellationToken);
                if (result.MatchedCount == 0)
                    throw new KeyNotFoundException($"No rollout found with id '{rolloutId}'.");
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to update counters for rollout '{rolloutId}'.", ex);
            }
        }
    }
}
