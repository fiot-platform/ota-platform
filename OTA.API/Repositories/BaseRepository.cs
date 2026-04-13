using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using OTA.API.Repositories.Interfaces;

namespace OTA.API.Repositories
{
    /// <summary>
    /// Abstract generic base repository providing standard MongoDB CRUD operations.
    /// All concrete repositories extend this class and receive a typed IMongoCollection.
    /// </summary>
    /// <typeparam name="T">The entity type. Must be a reference type with a string Id property
    /// decorated with [BsonId][BsonRepresentation(BsonType.ObjectId)].</typeparam>
    public abstract class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        /// <summary>
        /// The typed MongoDB collection used by this repository.
        /// </summary>
        protected readonly IMongoCollection<T> Collection;

        /// <summary>
        /// Initialises a new instance of <see cref="BaseRepository{T}"/> using the provided
        /// MongoDB database and collection name.
        /// </summary>
        /// <param name="database">The MongoDB database instance injected via DI.</param>
        /// <param name="collectionName">The name of the MongoDB collection that holds documents of type <typeparamref name="T"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="database"/> or <paramref name="collectionName"/> is null.</exception>
        protected BaseRepository(IMongoDatabase database, string collectionName)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            if (string.IsNullOrWhiteSpace(collectionName)) throw new ArgumentNullException(nameof(collectionName));

            Collection = database.GetCollection<T>(collectionName);
        }

        /// <inheritdoc/>
        public virtual async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id must not be null or empty.", nameof(id));

            try
            {
                var filter = Builders<T>.Filter.Eq("_id", ObjectId.Parse(id));
                return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            }
            catch (FormatException ex)
            {
                throw new ArgumentException($"The value '{id}' is not a valid ObjectId.", nameof(id), ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve document with id '{id}' from '{Collection.CollectionNamespace.CollectionName}'.", ex);
            }
        }

        /// <inheritdoc/>
        public virtual async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await Collection.Find(Builders<T>.Filter.Empty).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve all documents from '{Collection.CollectionNamespace.CollectionName}'.", ex);
            }
        }

        /// <inheritdoc/>
        public virtual async Task InsertAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            try
            {
                await Collection.InsertOneAsync(entity, new InsertOneOptions { BypassDocumentValidation = false }, cancellationToken);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                throw new InvalidOperationException($"A duplicate key violation occurred while inserting into '{Collection.CollectionNamespace.CollectionName}'. Detail: {ex.WriteError.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to insert document into '{Collection.CollectionNamespace.CollectionName}'.", ex);
            }
        }

        /// <inheritdoc/>
        public virtual async Task UpdateAsync(string id, T entity, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id must not be null or empty.", nameof(id));
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            try
            {
                var filter = Builders<T>.Filter.Eq("_id", ObjectId.Parse(id));
                var result = await Collection.ReplaceOneAsync(filter, entity, new ReplaceOptions { IsUpsert = false }, cancellationToken);

                if (result.MatchedCount == 0)
                    throw new KeyNotFoundException($"No document found with id '{id}' in '{Collection.CollectionNamespace.CollectionName}'.");
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (FormatException ex)
            {
                throw new ArgumentException($"The value '{id}' is not a valid ObjectId.", nameof(id), ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to update document with id '{id}' in '{Collection.CollectionNamespace.CollectionName}'.", ex);
            }
        }

        /// <inheritdoc/>
        public virtual async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id must not be null or empty.", nameof(id));

            try
            {
                var filter = Builders<T>.Filter.Eq("_id", ObjectId.Parse(id));
                var result = await Collection.DeleteOneAsync(filter, cancellationToken);
                return result.DeletedCount > 0;
            }
            catch (FormatException ex)
            {
                throw new ArgumentException($"The value '{id}' is not a valid ObjectId.", nameof(id), ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to delete document with id '{id}' from '{Collection.CollectionNamespace.CollectionName}'.", ex);
            }
        }

        /// <inheritdoc/>
        public virtual async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id must not be null or empty.", nameof(id));

            try
            {
                var filter = Builders<T>.Filter.Eq("_id", ObjectId.Parse(id));
                var count = await Collection.CountDocumentsAsync(filter, null, cancellationToken);
                return count > 0;
            }
            catch (FormatException ex)
            {
                throw new ArgumentException($"The value '{id}' is not a valid ObjectId.", nameof(id), ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to check existence of document with id '{id}' in '{Collection.CollectionNamespace.CollectionName}'.", ex);
            }
        }
    }
}
