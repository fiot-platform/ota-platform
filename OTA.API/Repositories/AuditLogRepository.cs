using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using OTA.API.Models.DTOs;
using OTA.API.Models.Entities;
using OTA.API.Repositories.Interfaces;

namespace OTA.API.Repositories
{
    /// <summary>
    /// MongoDB implementation of <see cref="IAuditLogRepository"/>.
    /// Creates a text index on PerformedByEmail and EntityType, plus regular indexes on Timestamp desc and Action.
    /// </summary>
    public class AuditLogRepository : BaseRepository<AuditLogEntity>, IAuditLogRepository
    {
        /// <summary>
        /// Initialises a new instance of <see cref="AuditLogRepository"/> and ensures required indexes exist.
        /// </summary>
        /// <param name="database">The MongoDB database instance.</param>
        public AuditLogRepository(IMongoDatabase database) : base(database, "AuditLogs")
        {
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var indexModels = new List<CreateIndexModel<AuditLogEntity>>
            {
                // Text index for full-text search across email and entity type
                new CreateIndexModel<AuditLogEntity>(
                    Builders<AuditLogEntity>.IndexKeys
                        .Text(a => a.PerformedByEmail)
                        .Text(a => a.EntityType),
                    new CreateIndexOptions { Name = "idx_auditlogs_text" }),

                new CreateIndexModel<AuditLogEntity>(
                    Builders<AuditLogEntity>.IndexKeys.Descending(a => a.Timestamp),
                    new CreateIndexOptions { Name = "idx_auditlogs_timestamp_desc" }),

                new CreateIndexModel<AuditLogEntity>(
                    Builders<AuditLogEntity>.IndexKeys.Ascending(a => a.Action),
                    new CreateIndexOptions { Name = "idx_auditlogs_action" }),

                new CreateIndexModel<AuditLogEntity>(
                    Builders<AuditLogEntity>.IndexKeys.Ascending(a => a.PerformedByUserId),
                    new CreateIndexOptions { Name = "idx_auditlogs_userId" }),

                new CreateIndexModel<AuditLogEntity>(
                    Builders<AuditLogEntity>.IndexKeys
                        .Ascending(a => a.EntityType)
                        .Ascending(a => a.EntityId),
                    new CreateIndexOptions { Name = "idx_auditlogs_entityType_entityId" })
            };

            Collection.Indexes.CreateMany(indexModels);
        }

        /// <summary>
        /// Builds a MongoDB filter definition from the structured <see cref="AuditLogFilterRequest"/>.
        /// </summary>
        private FilterDefinition<AuditLogEntity> BuildFilter(AuditLogFilterRequest filter)
        {
            var filters = new List<FilterDefinition<AuditLogEntity>>();

            if (filter.DateFrom.HasValue)
                filters.Add(Builders<AuditLogEntity>.Filter.Gte(a => a.Timestamp, filter.DateFrom.Value));

            if (filter.DateTo.HasValue)
                filters.Add(Builders<AuditLogEntity>.Filter.Lte(a => a.Timestamp, filter.DateTo.Value));

            if (filter.Action.HasValue)
                filters.Add(Builders<AuditLogEntity>.Filter.Eq(a => a.Action, filter.Action.Value));

            if (!string.IsNullOrWhiteSpace(filter.UserId))
                filters.Add(Builders<AuditLogEntity>.Filter.Eq(a => a.PerformedByUserId, filter.UserId));

            if (!string.IsNullOrWhiteSpace(filter.EntityType))
                filters.Add(Builders<AuditLogEntity>.Filter.Eq(a => a.EntityType, filter.EntityType));

            if (!string.IsNullOrWhiteSpace(filter.EntityId))
                filters.Add(Builders<AuditLogEntity>.Filter.Eq(a => a.EntityId, filter.EntityId));

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var regex = new MongoDB.Bson.BsonRegularExpression(filter.SearchText, "i");
                filters.Add(Builders<AuditLogEntity>.Filter.Or(
                    Builders<AuditLogEntity>.Filter.Regex(a => a.PerformedByEmail, regex),
                    Builders<AuditLogEntity>.Filter.Regex(a => a.EntityType, regex)
                ));
            }

            return filters.Count == 0
                ? Builders<AuditLogEntity>.Filter.Empty
                : Builders<AuditLogEntity>.Filter.And(filters);
        }

        /// <inheritdoc/>
        public async Task<List<AuditLogEntity>> SearchAsync(AuditLogFilterRequest filter, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;

            try
            {
                var skip = (page - 1) * pageSize;
                var mongoFilter = BuildFilter(filter);

                return await Collection.Find(mongoFilter)
                    .SortByDescending(a => a.Timestamp)
                    .Skip(skip)
                    .Limit(pageSize)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to search audit logs.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<AuditLogEntity>> ExportAsync(AuditLogFilterRequest filter, CancellationToken cancellationToken = default)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            try
            {
                var mongoFilter = BuildFilter(filter);
                return await Collection.Find(mongoFilter)
                    .SortByDescending(a => a.Timestamp)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to export audit logs.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<long> CountAsync(AuditLogFilterRequest filter, CancellationToken cancellationToken = default)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            try
            {
                var mongoFilter = BuildFilter(filter);
                return await Collection.CountDocumentsAsync(mongoFilter, null, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to count audit log entries.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<AuditLogEntity>> GetByEntityAsync(string entityType, string entityId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(entityType))
                throw new ArgumentException("EntityType must not be null or empty.", nameof(entityType));
            if (string.IsNullOrWhiteSpace(entityId))
                throw new ArgumentException("EntityId must not be null or empty.", nameof(entityId));

            try
            {
                var filter = Builders<AuditLogEntity>.Filter.And(
                    Builders<AuditLogEntity>.Filter.Eq(a => a.EntityType, entityType),
                    Builders<AuditLogEntity>.Filter.Eq(a => a.EntityId, entityId)
                );

                return await Collection.Find(filter)
                    .SortBy(a => a.Timestamp)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve audit logs for entity '{entityType}/{entityId}'.", ex);
            }
        }
    }
}
