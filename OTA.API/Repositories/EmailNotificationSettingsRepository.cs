using MongoDB.Bson;
using MongoDB.Driver;
using OTA.API.Models.Entities;
using OTA.API.Repositories.Interfaces;

namespace OTA.API.Repositories
{
    /// <summary>
    /// MongoDB implementation of <see cref="IEmailNotificationSettingsRepository"/>.
    /// Treats the collection as a singleton document store — at most one document exists.
    /// </summary>
    public class EmailNotificationSettingsRepository : IEmailNotificationSettingsRepository
    {
        private readonly IMongoCollection<EmailNotificationSettingsEntity> _collection;

        public EmailNotificationSettingsRepository(IMongoDatabase database)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));
            _collection = database.GetCollection<EmailNotificationSettingsEntity>("EmailNotificationSettings");
        }

        /// <inheritdoc/>
        public async Task<EmailNotificationSettingsEntity?> GetAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _collection
                    .Find(Builders<EmailNotificationSettingsEntity>.Filter.Empty)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve email notification settings.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task SaveAsync(EmailNotificationSettingsEntity settings, CancellationToken cancellationToken = default)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            try
            {
                FilterDefinition<EmailNotificationSettingsEntity> filter;

                if (!string.IsNullOrWhiteSpace(settings.Id))
                {
                    filter = Builders<EmailNotificationSettingsEntity>.Filter.Eq("_id", ObjectId.Parse(settings.Id));
                }
                else
                {
                    // No Id yet — assign a new one and use an impossible filter so upsert creates a fresh doc.
                    settings.Id = ObjectId.GenerateNewId().ToString();
                    filter = Builders<EmailNotificationSettingsEntity>.Filter.Eq("_id", ObjectId.Parse(settings.Id));
                }

                var options = new ReplaceOptions { IsUpsert = true };
                await _collection.ReplaceOneAsync(filter, settings, options, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save email notification settings.", ex);
            }
        }
    }
}
