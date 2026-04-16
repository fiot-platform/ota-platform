using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using OTA.API.Models.Entities;
using OTA.API.Repositories.Interfaces;

namespace OTA.API.Repositories
{
    /// <summary>
    /// MongoDB implementation of <see cref="IRepositoryMasterRepository"/>.
    /// Creates indexes on ProjectId and a unique index on GiteaRepoId at construction time.
    /// </summary>
    public class RepositoryMasterRepository : BaseRepository<RepositoryMasterEntity>, IRepositoryMasterRepository
    {
        /// <summary>
        /// Initialises a new instance of <see cref="RepositoryMasterRepository"/> and ensures required indexes exist.
        /// </summary>
        /// <param name="database">The MongoDB database instance.</param>
        public RepositoryMasterRepository(IMongoDatabase database) : base(database, "RepositoryMasters")
        {
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var indexModels = new List<CreateIndexModel<RepositoryMasterEntity>>
            {
                new CreateIndexModel<RepositoryMasterEntity>(
                    Builders<RepositoryMasterEntity>.IndexKeys.Ascending(r => r.ProjectId),
                    new CreateIndexOptions { Name = "idx_repomaster_projectId" }),

                new CreateIndexModel<RepositoryMasterEntity>(
                    Builders<RepositoryMasterEntity>.IndexKeys.Ascending(r => r.GiteaRepoId),
                    new CreateIndexOptions { Unique = true, Name = "idx_repomaster_giteaRepoId_unique" }),

                new CreateIndexModel<RepositoryMasterEntity>(
                    Builders<RepositoryMasterEntity>.IndexKeys.Ascending(r => r.IsActive),
                    new CreateIndexOptions { Name = "idx_repomaster_isActive" })
            };

            Collection.Indexes.CreateMany(indexModels);
        }

        /// <inheritdoc/>
        public async Task<List<RepositoryMasterEntity>> GetByProjectIdAsync(string projectId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(projectId))
                throw new ArgumentException("ProjectId must not be null or empty.", nameof(projectId));

            try
            {
                var filter = Builders<RepositoryMasterEntity>.Filter.Eq(r => r.ProjectId, projectId);
                return await Collection.Find(filter).SortBy(r => r.GiteaRepoName).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve repositories for project '{projectId}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<RepositoryMasterEntity?> GetByGiteaRepoIdAsync(string giteaRepoId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(giteaRepoId))
                throw new ArgumentException("GiteaRepoId must not be null or empty.", nameof(giteaRepoId));

            try
            {
                var filter = Builders<RepositoryMasterEntity>.Filter.Eq(r => r.GiteaRepoId, giteaRepoId);
                return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve repository with GiteaRepoId '{giteaRepoId}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<RepositoryMasterEntity>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<RepositoryMasterEntity>.Filter.Eq(r => r.IsActive, true);
                return await Collection.Find(filter).SortBy(r => r.GiteaRepoName).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve active repositories.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<RepositoryMasterEntity>> SearchAsync(string filter, int page, int pageSize, List<string>? allowedProjectIds = null, CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            try
            {
                var skip = (page - 1) * pageSize;
                var filters = new List<FilterDefinition<RepositoryMasterEntity>>();

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    var regex = new MongoDB.Bson.BsonRegularExpression(filter, "i");
                    filters.Add(Builders<RepositoryMasterEntity>.Filter.Or(
                        Builders<RepositoryMasterEntity>.Filter.Regex(r => r.GiteaRepoName, regex),
                        Builders<RepositoryMasterEntity>.Filter.Regex(r => r.GiteaUrl, regex)
                    ));
                }

                // null = no restriction; empty list = no projects allowed (return nothing)
                if (allowedProjectIds != null)
                    filters.Add(Builders<RepositoryMasterEntity>.Filter.In(r => r.ProjectId, allowedProjectIds));

                var mongoFilter = filters.Count > 0
                    ? Builders<RepositoryMasterEntity>.Filter.And(filters)
                    : Builders<RepositoryMasterEntity>.Filter.Empty;

                return await Collection.Find(mongoFilter)
                    .SortBy(r => r.GiteaRepoName)
                    .Skip(skip)
                    .Limit(pageSize)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to search repositories with filter '{filter}'.", ex);
            }
        }
    }
}
