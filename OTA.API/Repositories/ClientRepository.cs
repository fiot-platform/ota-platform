using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using OTA.API.Models.Entities;
using OTA.API.Repositories.Interfaces;

namespace OTA.API.Repositories
{
    /// <summary>
    /// MongoDB implementation of <see cref="IClientRepository"/>.
    /// Creates unique indexes on Code and ClientId plus a single-field index on Name.
    /// </summary>
    public class ClientRepository : BaseRepository<ClientEntity>, IClientRepository
    {
        public ClientRepository(IMongoDatabase database) : base(database, "clients")
        {
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            var indexes = new List<CreateIndexModel<ClientEntity>>
            {
                new(Builders<ClientEntity>.IndexKeys.Ascending(c => c.ClientId),
                    new CreateIndexOptions { Unique = true, Name = "idx_clients_clientId_unique" }),

                new(Builders<ClientEntity>.IndexKeys.Ascending(c => c.Code),
                    new CreateIndexOptions { Unique = true, Name = "idx_clients_code_unique" }),

                new(Builders<ClientEntity>.IndexKeys.Ascending(c => c.Name),
                    new CreateIndexOptions { Name = "idx_clients_name" }),

                new(Builders<ClientEntity>.IndexKeys.Ascending(c => c.IsActive),
                    new CreateIndexOptions { Name = "idx_clients_isActive" }),
            };
            Collection.Indexes.CreateMany(indexes);
        }

        public async Task<ClientEntity?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Code must not be null or empty.", nameof(code));

            var filter = Builders<ClientEntity>.Filter.Regex(c => c.Code,
                new BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(code.Trim())}$", "i"));
            return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<ClientEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name must not be null or empty.", nameof(name));

            var filter = Builders<ClientEntity>.Filter.Regex(c => c.Name,
                new BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(name.Trim())}$", "i"));
            return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<ClientEntity>> SearchAsync(string filter, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var mongoFilter = BuildSearchFilter(filter);
            return await Collection.Find(mongoFilter)
                .SortBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<long> CountAsync(string filter, CancellationToken cancellationToken = default)
        {
            var mongoFilter = BuildSearchFilter(filter);
            return await Collection.CountDocumentsAsync(mongoFilter, null, cancellationToken);
        }

        public async Task<string> GetNextCodeAsync(string prefix = "CUSTOM_", CancellationToken cancellationToken = default)
        {
            var escapedPrefix = Regex.Escape(prefix.ToUpperInvariant());
            var pattern       = new BsonRegularExpression($"^{escapedPrefix}(\\d+)$");
            var filter        = Builders<ClientEntity>.Filter.Regex(c => c.Code, pattern);

            // Project only the Code field to avoid pulling full documents
            var codes = await Collection
                .Find(filter)
                .Project(Builders<ClientEntity>.Projection.Expression(c => c.Code))
                .ToListAsync(cancellationToken);

            var max = codes
                .Select(code =>
                {
                    var numStr = code.Substring(prefix.Length);
                    return int.TryParse(numStr, out var n) ? n : 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            return $"{prefix.ToUpperInvariant()}{max + 1:D5}";
        }

        private static FilterDefinition<ClientEntity> BuildSearchFilter(string filter)
        {
            var mongoFilter = Builders<ClientEntity>.Filter.Empty;
            if (!string.IsNullOrWhiteSpace(filter))
            {
                var regex = new BsonRegularExpression(Regex.Escape(filter.Trim()), "i");
                mongoFilter = Builders<ClientEntity>.Filter.Or(
                    Builders<ClientEntity>.Filter.Regex(c => c.Name, regex),
                    Builders<ClientEntity>.Filter.Regex(c => c.Code, regex),
                    Builders<ClientEntity>.Filter.Regex(c => c.ContactEmail!, regex)
                );
            }
            return mongoFilter;
        }
    }
}
