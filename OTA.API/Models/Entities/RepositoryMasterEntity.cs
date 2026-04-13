using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OTA.API.Models.Entities
{
    /// <summary>
    /// MongoDB document representing a Gitea repository registered with the OTA platform.
    /// This is the canonical entity used by repositories and services; RepositoryEntity is the
    /// simplified version. The canonical entity stored in the repository_masters collection.
    /// Collection: repository_masters
    /// </summary>
    public sealed class RepositoryMasterEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("repositoryId")]
        public string RepositoryId { get; set; } = string.Empty;

        [BsonElement("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [BsonElement("giteaRepoId")]
        public string GiteaRepoId { get; set; } = string.Empty;

        [BsonElement("giteaRepoName")]
        public string GiteaRepoName { get; set; } = string.Empty;

        [BsonElement("giteaOwner")]
        public string GiteaOwner { get; set; } = string.Empty;

        [BsonElement("giteaUrl")]
        [BsonIgnoreIfNull]
        public string? GiteaUrl { get; set; }

        [BsonElement("giteaCloneUrl")]
        public string GiteaCloneUrl { get; set; } = string.Empty;

        [BsonElement("description")]
        [BsonIgnoreIfNull]
        public string? Description { get; set; }

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("lastSyncedAt")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? LastSyncedAt { get; set; }

        [BsonElement("defaultBranch")]
        public string DefaultBranch { get; set; } = "main";

        [BsonElement("topics")]
        public List<string> Topics { get; set; } = new();

        [BsonElement("firmwareVersionCount")]
        public int FirmwareVersionCount { get; set; }

        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("createdByUserId")]
        public string CreatedByUserId { get; set; } = string.Empty;

        [BsonElement("webhookConfigured")]
        public bool WebhookConfigured { get; set; } = false;

        // ── Alias properties for service layer compatibility ───────────────────
        [BsonIgnore] public string Name { get => GiteaRepoName; set => GiteaRepoName = value; }
    }
}
