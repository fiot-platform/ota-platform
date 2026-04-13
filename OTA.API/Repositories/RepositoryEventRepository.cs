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
    /// MongoDB implementation of <see cref="IRepositoryEventRepository"/>.
    /// Creates indexes on GiteaRepoId, Status, and ReceivedAt (TTL-friendly descending).
    /// </summary>
    public class RepositoryEventRepository : BaseRepository<RepositoryEventEntity>, IRepositoryEventRepository
    {
        /// <summary>
        /// Initialises a new instance of <see cref="RepositoryEventRepository"/> and ensures required indexes exist.
        /// </summary>
        /// <param name="database">The MongoDB database instance.</param>
        public RepositoryEventRepository(IMongoDatabase database) : base(database, "RepositoryEvents")
        {
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var indexModels = new List<CreateIndexModel<RepositoryEventEntity>>
            {
                new CreateIndexModel<RepositoryEventEntity>(
                    Builders<RepositoryEventEntity>.IndexKeys.Ascending(e => e.GiteaRepoId),
                    new CreateIndexOptions { Name = "idx_repoevents_giteaRepoId" }),

                new CreateIndexModel<RepositoryEventEntity>(
                    Builders<RepositoryEventEntity>.IndexKeys.Ascending(e => e.Status),
                    new CreateIndexOptions { Name = "idx_repoevents_status" }),

                // Descending index on ReceivedAt for recent event queries and TTL-style pruning
                new CreateIndexModel<RepositoryEventEntity>(
                    Builders<RepositoryEventEntity>.IndexKeys.Descending(e => e.ReceivedAt),
                    new CreateIndexOptions { Name = "idx_repoevents_receivedAt_desc" }),

                new CreateIndexModel<RepositoryEventEntity>(
                    Builders<RepositoryEventEntity>.IndexKeys.Ascending(e => e.RetryCount),
                    new CreateIndexOptions { Name = "idx_repoevents_retryCount" }),

                new CreateIndexModel<RepositoryEventEntity>(
                    Builders<RepositoryEventEntity>.IndexKeys.Ascending(e => e.DeliveryId),
                    new CreateIndexOptions { Unique = true, Sparse = true, Name = "idx_repoevents_deliveryId_unique" })
            };

            Collection.Indexes.CreateMany(indexModels);
        }

        /// <inheritdoc/>
        public async Task<List<RepositoryEventEntity>> GetByGiteaRepoIdAsync(string giteaRepoId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(giteaRepoId))
                throw new ArgumentException("GiteaRepoId must not be null or empty.", nameof(giteaRepoId));

            try
            {
                FilterDefinition<RepositoryEventEntity> filter;
                if (long.TryParse(giteaRepoId, out var giteaRepoIdLong))
                    filter = Builders<RepositoryEventEntity>.Filter.Eq(e => e.GiteaRepoId, giteaRepoIdLong);
                else
                    filter = Builders<RepositoryEventEntity>.Filter.Empty;
                return await Collection.Find(filter)
                    .SortByDescending(e => e.ReceivedAt)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve events for Gitea repo '{giteaRepoId}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<RepositoryEventEntity>> GetByStatusAsync(WebhookEventStatus status, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<RepositoryEventEntity>.Filter.Eq(e => e.Status, status);
                return await Collection.Find(filter)
                    .SortByDescending(e => e.ReceivedAt)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve events with status '{status}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<RepositoryEventEntity>> GetFailedEventsAsync(int maxRetries = 3, int limit = 50, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<RepositoryEventEntity>.Filter.And(
                    Builders<RepositoryEventEntity>.Filter.Eq(e => e.Status, WebhookEventStatus.Failed),
                    Builders<RepositoryEventEntity>.Filter.Lt(e => e.RetryCount, maxRetries)
                );

                return await Collection.Find(filter)
                    .SortBy(e => e.ReceivedAt)
                    .Limit(limit)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve failed events eligible for retry.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<RepositoryEventEntity>> GetRecentEventsAsync(int limit = 20, CancellationToken cancellationToken = default)
        {
            try
            {
                return await Collection.Find(Builders<RepositoryEventEntity>.Filter.Empty)
                    .SortByDescending(e => e.ReceivedAt)
                    .Limit(limit)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve recent repository events.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<Dictionary<WebhookEventStatus, long>> CountByStatusAsync(CancellationToken cancellationToken = default)
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

                var dict = new Dictionary<WebhookEventStatus, long>();
                foreach (var doc in results)
                {
                    if (Enum.TryParse<WebhookEventStatus>(doc["_id"].AsString, out var status))
                        dict[status] = doc["count"].AsInt64;
                }

                foreach (WebhookEventStatus status in Enum.GetValues(typeof(WebhookEventStatus)))
                {
                    if (!dict.ContainsKey(status))
                        dict[status] = 0;
                }

                return dict;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to count repository events by status.", ex);
            }
        }
    }
}
