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
    /// MongoDB implementation of <see cref="IRolloutPolicyRepository"/>.
    /// Creates indexes on IsActive and Name (unique) at construction time.
    /// </summary>
    public class RolloutPolicyRepository : BaseRepository<RolloutPolicyEntity>, IRolloutPolicyRepository
    {
        /// <summary>
        /// Initialises a new instance of <see cref="RolloutPolicyRepository"/> and ensures required indexes exist.
        /// </summary>
        /// <param name="database">The MongoDB database instance.</param>
        public RolloutPolicyRepository(IMongoDatabase database) : base(database, "RolloutPolicies")
        {
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var indexModels = new List<CreateIndexModel<RolloutPolicyEntity>>
            {
                new CreateIndexModel<RolloutPolicyEntity>(
                    Builders<RolloutPolicyEntity>.IndexKeys.Ascending(p => p.Name),
                    new CreateIndexOptions { Unique = true, Name = "idx_rolloutpolicies_name_unique" }),

                new CreateIndexModel<RolloutPolicyEntity>(
                    Builders<RolloutPolicyEntity>.IndexKeys.Ascending(p => p.IsActive),
                    new CreateIndexOptions { Name = "idx_rolloutpolicies_isActive" })
            };

            Collection.Indexes.CreateMany(indexModels);
        }

        /// <inheritdoc/>
        public async Task<List<RolloutPolicyEntity>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<RolloutPolicyEntity>.Filter.Eq(p => p.IsActive, true);
                return await Collection.Find(filter).SortBy(p => p.Name).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve active rollout policies.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<RolloutPolicyEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name must not be null or empty.", nameof(name));

            try
            {
                var filter = Builders<RolloutPolicyEntity>.Filter.Regex(p => p.Name,
                    new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(name)}$", "i"));
                return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve rollout policy with name '{name}'.", ex);
            }
        }
    }
}
