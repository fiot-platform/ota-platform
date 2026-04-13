using BCrypt.Net;
using MongoDB.Driver;
using OTA.API.Models.Entities;
using OTA.API.Models.Enums;
using OTA.API.Models.Settings;
using Microsoft.Extensions.Options;

namespace OTA.API.Helpers
{
    /// <summary>
    /// Runs once at application startup to seed the default SuperAdmin account
    /// if the Users collection is empty.
    /// </summary>
    public static class DataSeeder
    {
        public static async Task SeedAsync(IServiceProvider services, ILogger logger)
        {
            var db       = services.GetRequiredService<IMongoDatabase>();
            var settings = services.GetRequiredService<IOptions<MongoDbSettings>>().Value;

            // Must match the collection name used by UserRepository ("Users")
            var usersCollection = db.GetCollection<UserEntity>("Users");

            long count = await usersCollection.CountDocumentsAsync(FilterDefinition<UserEntity>.Empty);

            if (count > 0)
            {
                logger.LogInformation("DataSeeder: {Count} user(s) already exist — skipping seed.", count);
                return;
            }

            const string defaultEmail    = "admin@ota.local";
            const string defaultPassword = "Admin@123!";
            const string defaultName     = "Super Admin";

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword, workFactor: 12);

            var superAdmin = new UserEntity
            {
                UserId       = Guid.NewGuid().ToString(),
                Name         = defaultName,
                Email        = defaultEmail,
                PasswordHash = passwordHash,
                Role         = UserRole.SuperAdmin,
                IsActive     = true,
                CreatedAt    = DateTime.UtcNow,
                UpdatedAt    = DateTime.UtcNow,
            };

            await usersCollection.InsertOneAsync(superAdmin);

            logger.LogWarning(
                "DataSeeder: Default SuperAdmin created. " +
                "Email: {Email} | Password: {Password} — CHANGE THIS IMMEDIATELY IN PRODUCTION.",
                defaultEmail, defaultPassword);
        }
    }
}
