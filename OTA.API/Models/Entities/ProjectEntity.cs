using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OTA.API.Models.Entities
{
    /// <summary>
    /// MongoDB document representing a customer project within the OTA platform.
    /// A project groups one or more Gitea repositories and their associated firmware releases and rollouts.
    /// Collection: projects
    /// Indexes:
    ///   - Unique: ProjectId
    ///   - Single: CustomerId
    ///   - Compound: {CustomerId, IsActive}
    ///   - Text: Name (for search)
    /// </summary>
    public sealed class ProjectEntity
    {
        /// <summary>MongoDB internal ObjectId (_id).</summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Platform-generated unique identifier for the project (GUID string).
        /// Referenced by RepositoryEntity, RolloutEntity, and FirmwareVersionEntity.
        /// </summary>
        [BsonElement("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>Human-readable project name; unique within a customer tenant.</summary>
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Optional free-text description of the project's purpose and scope.</summary>
        [BsonElement("description")]
        [BsonIgnoreIfNull]
        public string? Description { get; set; }

        /// <summary>Identifier of the customer tenant that owns this project.</summary>
        [BsonElement("customerId")]
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>Display name of the customer for denormalised reads (avoids a join).</summary>
        [BsonElement("customerName")]
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        /// Business unit or division within the customer organisation responsible for this project.
        /// Used for reporting and cost-centre allocation.
        /// </summary>
        [BsonElement("businessUnit")]
        [BsonIgnoreIfNull]
        public string? BusinessUnit { get; set; }

        /// <summary>
        /// Whether the project is active. Inactive projects are hidden from normal operations
        /// but retained for historical audit purposes.
        /// </summary>
        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// The Gitea organisation name under which all repositories for this project are hosted.
        /// Corresponds to the Gitea org slug (e.g., "acme-iot").
        /// </summary>
        [BsonElement("giteaOrgName")]
        [BsonIgnoreIfNull]
        public string? GiteaOrgName { get; set; }

        /// <summary>UTC timestamp when the project was created.</summary>
        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>UTC timestamp of the most recent update to this project record.</summary>
        [BsonElement("updatedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>UserId of the platform user who created this project.</summary>
        [BsonElement("createdByUserId")]
        public string CreatedByUserId { get; set; } = string.Empty;

        /// <summary>
        /// Free-form tags for categorisation and filtering (e.g., ["embedded", "production", "eu-region"]).
        /// </summary>
        [BsonElement("tags")]
        public List<string> Tags { get; set; } = new();
    }
}
