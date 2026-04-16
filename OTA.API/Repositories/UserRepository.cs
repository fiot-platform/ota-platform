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
    /// MongoDB implementation of <see cref="IUserRepository"/>.
    /// Creates unique index on Email and a standard index on Role at construction time.
    /// </summary>
    public class UserRepository : BaseRepository<UserEntity>, IUserRepository
    {
        /// <summary>
        /// Initialises a new instance of <see cref="UserRepository"/> and ensures required indexes exist.
        /// </summary>
        /// <param name="database">The MongoDB database instance.</param>
        public UserRepository(IMongoDatabase database) : base(database, "Users")
        {
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var indexModels = new List<CreateIndexModel<UserEntity>>
            {
                new CreateIndexModel<UserEntity>(
                    Builders<UserEntity>.IndexKeys.Ascending(u => u.Email),
                    new CreateIndexOptions { Unique = true, Name = "idx_users_email_unique" }),

                new CreateIndexModel<UserEntity>(
                    Builders<UserEntity>.IndexKeys.Ascending(u => u.Role),
                    new CreateIndexOptions { Name = "idx_users_role" }),

                new CreateIndexModel<UserEntity>(
                    Builders<UserEntity>.IndexKeys.Ascending(u => u.CustomerId),
                    new CreateIndexOptions { Name = "idx_users_customerId" }),

                new CreateIndexModel<UserEntity>(
                    Builders<UserEntity>.IndexKeys.Ascending(u => u.IsActive),
                    new CreateIndexOptions { Name = "idx_users_isActive" })
            };

            Collection.Indexes.CreateMany(indexModels);
        }

        /// <inheritdoc/>
        public async Task<UserEntity?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email must not be null or empty.", nameof(email));

            try
            {
                var filter = Builders<UserEntity>.Filter.Regex(u => u.Email,
                    new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(email)}$", "i"));
                return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve user by email '{email}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<UserEntity>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<UserEntity>.Filter.Eq(u => u.Role, role);
                return await Collection.Find(filter).SortBy(u => u.Name).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve users with role '{role}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<UserEntity>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(customerId))
                throw new ArgumentException("CustomerId must not be null or empty.", nameof(customerId));

            try
            {
                var filter = Builders<UserEntity>.Filter.Eq(u => u.CustomerId, customerId);
                return await Collection.Find(filter).SortBy(u => u.Name).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve users for customer '{customerId}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task UpdatePasswordAsync(string userId, string passwordHash, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId must not be null or empty.", nameof(userId));
            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("PasswordHash must not be null or empty.", nameof(passwordHash));

            try
            {
                var filter = Builders<UserEntity>.Filter.Eq("_id", ObjectId.Parse(userId));
                var update = Builders<UserEntity>.Update
                    .Set(u => u.PasswordHash, passwordHash)
                    .Set(u => u.UpdatedAt, DateTime.UtcNow);

                var result = await Collection.UpdateOneAsync(filter, update, null, cancellationToken);
                if (result.MatchedCount == 0)
                    throw new KeyNotFoundException($"No user found with id '{userId}'.");
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to update password for user '{userId}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task UpdateRefreshTokenAsync(string userId, string? refreshToken, DateTime expiresAt, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId must not be null or empty.", nameof(userId));

            try
            {
                var filter = Builders<UserEntity>.Filter.Eq("_id", ObjectId.Parse(userId));
                var update = Builders<UserEntity>.Update
                    .Set(u => u.RefreshToken, refreshToken)
                    .Set(u => u.RefreshTokenExpiry, expiresAt)
                    .Set(u => u.UpdatedAt, DateTime.UtcNow);

                var result = await Collection.UpdateOneAsync(filter, update, null, cancellationToken);
                if (result.MatchedCount == 0)
                    throw new KeyNotFoundException($"No user found with id '{userId}'.");
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to update refresh token for user '{userId}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<UserEntity>> SearchUsersAsync(string filter, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            try
            {
                var skip = (page - 1) * pageSize;
                FilterDefinition<UserEntity> mongoFilter;

                if (string.IsNullOrWhiteSpace(filter))
                {
                    mongoFilter = Builders<UserEntity>.Filter.Empty;
                }
                else
                {
                    var regex = new MongoDB.Bson.BsonRegularExpression(filter, "i");
                    mongoFilter = Builders<UserEntity>.Filter.Or(
                        Builders<UserEntity>.Filter.Regex(u => u.Name, regex),
                        Builders<UserEntity>.Filter.Regex(u => u.Email, regex),
                        Builders<UserEntity>.Filter.Regex(u => u.CustomerId, regex)
                    );
                }

                return await Collection.Find(mongoFilter)
                    .SortBy(u => u.Name)
                    .Skip(skip)
                    .Limit(pageSize)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to search users with filter '{filter}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<long> CountAsync(string filter, CancellationToken cancellationToken = default)
        {
            try
            {
                FilterDefinition<UserEntity> mongoFilter;

                if (string.IsNullOrWhiteSpace(filter))
                {
                    mongoFilter = Builders<UserEntity>.Filter.Empty;
                }
                else
                {
                    var regex = new MongoDB.Bson.BsonRegularExpression(filter, "i");
                    mongoFilter = Builders<UserEntity>.Filter.Or(
                        Builders<UserEntity>.Filter.Regex(u => u.Name, regex),
                        Builders<UserEntity>.Filter.Regex(u => u.Email, regex),
                        Builders<UserEntity>.Filter.Regex(u => u.CustomerId, regex)
                    );
                }

                return await Collection.CountDocumentsAsync(mongoFilter, null, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to count users with filter '{filter}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<UserEntity?> FindByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return null;

            try
            {
                var filter = Builders<UserEntity>.Filter.Eq(u => u.RefreshToken, refreshToken);
                return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to find user by refresh token.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task DeactivateAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId must not be null or empty.", nameof(userId));

            try
            {
                var filter = Builders<UserEntity>.Filter.Eq("_id", ObjectId.Parse(userId));
                var update = Builders<UserEntity>.Update
                    .Set(u => u.IsActive, false)
                    .Set(u => u.UpdatedAt, DateTime.UtcNow);

                var result = await Collection.UpdateOneAsync(filter, update, null, cancellationToken);
                if (result.MatchedCount == 0)
                    throw new KeyNotFoundException($"No user found with id '{userId}'.");
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to deactivate user '{userId}'.", ex);
            }
        }

        // ── FCM token management ──────────────────────────────────────────────

        /// <inheritdoc/>
        public async Task AddOrUpdateFcmTokenAsync(string userId, string token, string? deviceLabel, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId is required.", nameof(userId));
            if (string.IsNullOrWhiteSpace(token))  throw new ArgumentException("Token is required.",  nameof(token));

            try
            {
                var userFilter = Builders<UserEntity>.Filter.Eq(u => u.UserId, userId);

                // Pull any existing entry for this token, then push the refreshed entry.
                var pull = Builders<UserEntity>.Update
                    .PullFilter(u => u.FcmTokens, t => t.Token == token);
                await Collection.UpdateOneAsync(userFilter, pull, null, cancellationToken);

                var entry = new OTA.API.Models.Entities.FcmTokenEntry
                {
                    Token       = token,
                    DeviceLabel = deviceLabel,
                    RegisteredAt = DateTime.UtcNow
                };

                var push = Builders<UserEntity>.Update
                    .Push(u => u.FcmTokens, entry)
                    .Set(u => u.UpdatedAt, DateTime.UtcNow);

                await Collection.UpdateOneAsync(userFilter, push, null, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to add FCM token for user '{userId}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task RemoveFcmTokenAsync(string userId, string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId is required.", nameof(userId));
            if (string.IsNullOrWhiteSpace(token))  throw new ArgumentException("Token is required.",  nameof(token));

            try
            {
                var filter = Builders<UserEntity>.Filter.Eq(u => u.UserId, userId);
                var update = Builders<UserEntity>.Update
                    .PullFilter(u => u.FcmTokens, t => t.Token == token)
                    .Set(u => u.UpdatedAt, DateTime.UtcNow);

                await Collection.UpdateOneAsync(filter, update, null, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to remove FCM token for user '{userId}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task RemoveFcmTokenGloballyAsync(string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Token is required.", nameof(token));

            try
            {
                var filter = Builders<UserEntity>.Filter.ElemMatch(u => u.FcmTokens, t => t.Token == token);
                var update = Builders<UserEntity>.Update
                    .PullFilter(u => u.FcmTokens, t => t.Token == token)
                    .Set(u => u.UpdatedAt, DateTime.UtcNow);

                await Collection.UpdateManyAsync(filter, update, null, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to globally remove FCM token.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<UserEntity>> GetByRolesAsync(IEnumerable<UserRole> roles, CancellationToken cancellationToken = default)
        {
            try
            {
                var roleList = roles?.ToList() ?? new List<UserRole>();
                if (!roleList.Any()) return new List<UserEntity>();

                var filter = Builders<UserEntity>.Filter.In(u => u.Role, roleList);
                return await Collection.Find(filter).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve users by roles.", ex);
            }
        }
    }
}
