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
    /// MongoDB implementation of <see cref="IProjectRepository"/>.
    /// Creates indexes on CustomerId and IsActive at construction time.
    /// </summary>
    public class ProjectRepository : BaseRepository<ProjectEntity>, IProjectRepository
    {
        /// <summary>
        /// Initialises a new instance of <see cref="ProjectRepository"/> and ensures required indexes exist.
        /// </summary>
        /// <param name="database">The MongoDB database instance.</param>
        public ProjectRepository(IMongoDatabase database) : base(database, "Projects")
        {
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var indexModels = new List<CreateIndexModel<ProjectEntity>>
            {
                new CreateIndexModel<ProjectEntity>(
                    Builders<ProjectEntity>.IndexKeys.Ascending(p => p.CustomerId),
                    new CreateIndexOptions { Name = "idx_projects_customerId" }),

                new CreateIndexModel<ProjectEntity>(
                    Builders<ProjectEntity>.IndexKeys.Ascending(p => p.IsActive),
                    new CreateIndexOptions { Name = "idx_projects_isActive" }),

                new CreateIndexModel<ProjectEntity>(
                    Builders<ProjectEntity>.IndexKeys.Ascending(p => p.Name),
                    new CreateIndexOptions { Name = "idx_projects_name" })
            };

            Collection.Indexes.CreateMany(indexModels);
        }

        /// <inheritdoc/>
        public async Task<List<ProjectEntity>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(customerId))
                throw new ArgumentException("CustomerId must not be null or empty.", nameof(customerId));

            try
            {
                var filter = Builders<ProjectEntity>.Filter.Eq(p => p.CustomerId, customerId);
                return await Collection.Find(filter).SortBy(p => p.Name).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve projects for customer '{customerId}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<ProjectEntity>> GetActiveProjectsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<ProjectEntity>.Filter.Eq(p => p.IsActive, true);
                return await Collection.Find(filter).SortBy(p => p.Name).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve active projects.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<ProjectEntity>> SearchAsync(string filter, int page, int pageSize, List<string>? allowedProjectIds = null, CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            try
            {
                var skip = (page - 1) * pageSize;
                FilterDefinition<ProjectEntity> mongoFilter;

                if (string.IsNullOrWhiteSpace(filter))
                {
                    mongoFilter = Builders<ProjectEntity>.Filter.Empty;
                }
                else
                {
                    var regex = new MongoDB.Bson.BsonRegularExpression(filter, "i");
                    mongoFilter = Builders<ProjectEntity>.Filter.Or(
                        Builders<ProjectEntity>.Filter.Regex(p => p.Name, regex),
                        Builders<ProjectEntity>.Filter.Regex(p => p.Description, regex)
                    );
                }

                if (allowedProjectIds != null)
                    mongoFilter &= Builders<ProjectEntity>.Filter.In(p => p.Id, allowedProjectIds);

                return await Collection.Find(mongoFilter)
                    .SortBy(p => p.Name)
                    .Skip(skip)
                    .Limit(pageSize)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to search projects with filter '{filter}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<ProjectEntity?> GetByProjectIdAsync(string projectId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(projectId))
                throw new ArgumentException("ProjectId must not be null or empty.", nameof(projectId));

            try
            {
                var filter = Builders<ProjectEntity>.Filter.Eq(p => p.ProjectId, projectId);
                return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve project with projectId '{projectId}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<long> CountAsync(string filter, List<string>? allowedProjectIds = null, CancellationToken cancellationToken = default)
        {
            try
            {
                FilterDefinition<ProjectEntity> mongoFilter;

                if (string.IsNullOrWhiteSpace(filter))
                {
                    mongoFilter = Builders<ProjectEntity>.Filter.Empty;
                }
                else
                {
                    var regex = new MongoDB.Bson.BsonRegularExpression(filter, "i");
                    mongoFilter = Builders<ProjectEntity>.Filter.Or(
                        Builders<ProjectEntity>.Filter.Regex(p => p.Name, regex),
                        Builders<ProjectEntity>.Filter.Regex(p => p.Description, regex)
                    );
                }

                if (allowedProjectIds != null)
                    mongoFilter &= Builders<ProjectEntity>.Filter.In(p => p.Id, allowedProjectIds);

                return await Collection.CountDocumentsAsync(mongoFilter, null, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to count projects with filter '{filter}'.", ex);
            }
        }
    }
}
