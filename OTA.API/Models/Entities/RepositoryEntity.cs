using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OTA.API.Models.Entities
{
    /// <summary>
    /// MongoDB document representing a Gitea repository registered with the OTA platform.
    /// Each repository is the source of firmware binaries for its parent project.
    /// Collection: repositories
    /// Indexes:
    ///   - Unique: RepositoryId
    ///   - Unique: GiteaRepoId
    ///   - Single: ProjectId
    ///   - Single: GiteaOwner
    ///   - Compound: {ProjectId, IsActive}
    /// </summary>
    public sealed class RepositoryEntity
    {
        /// <summary>MongoDB internal ObjectId (_id).</summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Platform-generated unique identifier for the repository (GUID string).</summary>
        [BsonElement("repositoryId")]
        public string RepositoryId { get; set; } = string.Empty;

        /// <summary>ProjectId of the parent project that this repository belongs to.</summary>
        [BsonElement("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>
        /// Gitea's internal integer repository ID.
        /// Used as the stable key for webhook event correlation and API calls.
        /// </summary>
        [BsonElement("giteaRepoId")]
        public long GiteaRepoId { get; set; }

        /// <summary>The short name of the Gitea repository (e.g., "firmware-edge-gateway").</summary>
        [BsonElement("giteaRepoName")]
        public string GiteaRepoName { get; set; } = string.Empty;

        /// <summary>
        /// The Gitea owner of the repository — either a user login or an organisation name.
        /// Combined with GiteaRepoName forms the full repository path (owner/repo).
        /// </summary>
        [BsonElement("giteaOwner")]
        public string GiteaOwner { get; set; } = string.Empty;

        /// <summary>
        /// HTTPS clone URL provided by Gitea (e.g., https://gitea.internal/acme-iot/firmware-edge-gateway.git).
        /// Used to construct asset download URLs.
        /// </summary>
        [BsonElement("giteaCloneUrl")]
        public string GiteaCloneUrl { get; set; } = string.Empty;

        /// <summary>Optional free-text description of the repository and the device family it targets.</summary>
        [BsonElement("description")]
        [BsonIgnoreIfNull]
        public string? Description { get; set; }

        /// <summary>
        /// Whether this repository is active on the platform.
        /// Inactive repositories will not receive webhook processing and are excluded from firmware syncs.
        /// </summary>
        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        /// <summary>UTC timestamp of the most recent successful synchronisation with Gitea (releases/tags pulled).</summary>
        [BsonElement("lastSyncedAt")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? LastSyncedAt { get; set; }

        /// <summary>UTC timestamp when this repository was registered on the platform.</summary>
        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>UTC timestamp of the most recent update to this repository record.</summary>
        [BsonElement("updatedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>UserId of the platform user who registered this repository.</summary>
        [BsonElement("createdByUserId")]
        public string CreatedByUserId { get; set; } = string.Empty;

        /// <summary>Default branch name (e.g., "main" or "master") used for sync operations.</summary>
        [BsonElement("defaultBranch")]
        public string DefaultBranch { get; set; } = "main";

        /// <summary>
        /// Gitea repository topics (tags) used for discoverability and filtering
        /// (e.g., ["edge", "arm-cortex", "production"]).
        /// </summary>
        [BsonElement("topics")]
        public List<string> Topics { get; set; } = new();
    }
}
