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
    /// MongoDB implementation of <see cref="IOtaJobRepository"/>.
    /// Creates indexes on RolloutId, DeviceId, and Status. Supports bulk insert for batch job creation.
    /// </summary>
    public class OtaJobRepository : BaseRepository<OtaJobEntity>, IOtaJobRepository
    {
        /// <summary>
        /// Initialises a new instance of <see cref="OtaJobRepository"/> and ensures required indexes exist.
        /// </summary>
        /// <param name="database">The MongoDB database instance.</param>
        public OtaJobRepository(IMongoDatabase database) : base(database, "OtaJobs")
        {
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var indexModels = new List<CreateIndexModel<OtaJobEntity>>
            {
                new CreateIndexModel<OtaJobEntity>(
                    Builders<OtaJobEntity>.IndexKeys.Ascending(j => j.RolloutId),
                    new CreateIndexOptions { Name = "idx_otajobs_rolloutId" }),

                new CreateIndexModel<OtaJobEntity>(
                    Builders<OtaJobEntity>.IndexKeys.Ascending(j => j.DeviceId),
                    new CreateIndexOptions { Name = "idx_otajobs_deviceId" }),

                new CreateIndexModel<OtaJobEntity>(
                    Builders<OtaJobEntity>.IndexKeys.Ascending(j => j.Status),
                    new CreateIndexOptions { Name = "idx_otajobs_status" }),

                new CreateIndexModel<OtaJobEntity>(
                    Builders<OtaJobEntity>.IndexKeys
                        .Ascending(j => j.RolloutId)
                        .Ascending(j => j.Status),
                    new CreateIndexOptions { Name = "idx_otajobs_rolloutId_status" }),

                new CreateIndexModel<OtaJobEntity>(
                    Builders<OtaJobEntity>.IndexKeys.Ascending(j => j.CreatedAt),
                    new CreateIndexOptions { Name = "idx_otajobs_createdAt" })
            };

            Collection.Indexes.CreateMany(indexModels);
        }

        /// <summary>
        /// Accepts either a MongoDB ObjectId string or a platform-generated JobId GUID.
        /// Tries ObjectId first; falls back to the <c>jobId</c> field so callers that hold
        /// either identifier can resolve the job without knowing which format they have.
        /// </summary>
        public override async Task<OtaJobEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id must not be null or empty.", nameof(id));

            try
            {
                // 1. Try MongoDB ObjectId
                if (ObjectId.TryParse(id, out var objectId))
                {
                    var byOid = await Collection
                        .Find(Builders<OtaJobEntity>.Filter.Eq("_id", objectId))
                        .FirstOrDefaultAsync(cancellationToken);
                    if (byOid != null) return byOid;
                }

                // 2. Fall back to GUID JobId field
                return await Collection
                    .Find(Builders<OtaJobEntity>.Filter.Eq(j => j.JobId, id))
                    .FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve OTA job '{id}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<OtaJobEntity>> GetByRolloutIdAsync(string rolloutId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rolloutId))
                throw new ArgumentException("RolloutId must not be null or empty.", nameof(rolloutId));

            try
            {
                var filter = Builders<OtaJobEntity>.Filter.Eq(j => j.RolloutId, rolloutId);
                return await Collection.Find(filter).SortBy(j => j.CreatedAt).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve OTA jobs for rollout '{rolloutId}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<OtaJobEntity>> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                throw new ArgumentException("DeviceId must not be null or empty.", nameof(deviceId));

            try
            {
                var filter = Builders<OtaJobEntity>.Filter.Eq(j => j.DeviceId, deviceId);
                return await Collection.Find(filter).SortByDescending(j => j.CreatedAt).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve OTA jobs for device '{deviceId}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<OtaJobEntity>> GetByStatusAsync(OtaJobStatus status, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<OtaJobEntity>.Filter.Eq(j => j.Status, status);
                return await Collection.Find(filter).SortBy(j => j.CreatedAt).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve OTA jobs with status '{status}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<OtaJobEntity>> GetPendingJobsAsync(int limit = 100, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<OtaJobEntity>.Filter.Eq(j => j.Status, OtaJobStatus.Pending);
                return await Collection.Find(filter)
                    .SortBy(j => j.CreatedAt)
                    .Limit(limit)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve pending OTA jobs.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<Dictionary<OtaJobStatus, long>> CountByStatusAsync(string rolloutId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rolloutId))
                throw new ArgumentException("RolloutId must not be null or empty.", nameof(rolloutId));

            try
            {
                var pipeline = new[]
                {
                    new BsonDocument("$match", new BsonDocument("RolloutId", rolloutId)),
                    new BsonDocument("$group", new BsonDocument
                    {
                        { "_id", "$Status" },
                        { "count", new BsonDocument("$sum", 1) }
                    })
                };

                var results = await Collection.Aggregate<BsonDocument>(pipeline, null, cancellationToken).ToListAsync(cancellationToken);

                var dict = new Dictionary<OtaJobStatus, long>();
                foreach (var doc in results)
                {
                    if (Enum.TryParse<OtaJobStatus>(doc["_id"].AsString, out var status))
                        dict[status] = doc["count"].AsInt64;
                }

                foreach (OtaJobStatus status in Enum.GetValues(typeof(OtaJobStatus)))
                {
                    if (!dict.ContainsKey(status))
                        dict[status] = 0;
                }

                return dict;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to count OTA jobs by status for rollout '{rolloutId}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task BulkInsertAsync(List<OtaJobEntity> jobs, CancellationToken cancellationToken = default)
        {
            if (jobs == null) throw new ArgumentNullException(nameof(jobs));
            if (jobs.Count == 0) return;

            try
            {
                var writeModels = new List<WriteModel<OtaJobEntity>>(jobs.Count);
                foreach (var job in jobs)
                    writeModels.Add(new InsertOneModel<OtaJobEntity>(job));

                var options = new BulkWriteOptions { IsOrdered = false, BypassDocumentValidation = false };
                await Collection.BulkWriteAsync(writeModels, options, cancellationToken);
            }
            catch (MongoBulkWriteException<OtaJobEntity> ex)
            {
                throw new InvalidOperationException(
                    $"Bulk insert of OTA jobs partially failed. {ex.WriteErrors.Count} error(s) occurred. First error: {ex.WriteErrors[0].Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to bulk insert {jobs.Count} OTA jobs.", ex);
            }
        }
    }
}
