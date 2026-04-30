using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OTA.API.Models.Entities
{
    /// <summary>
    /// MongoDB document representing a client (customer organisation) in the OTA platform.
    /// Collection: clients
    /// Indexes:
    ///   - Unique: ClientId
    ///   - Unique: Code
    ///   - Single: Name
    ///   - Single: IsActive
    /// </summary>
    public sealed class ClientEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Platform-generated unique identifier (GUID string).</summary>
        [BsonElement("clientId")]
        public string ClientId { get; set; } = string.Empty;

        /// <summary>Full display name of the client organisation.</summary>
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Short unique code used as CustomerCode in device registration (e.g., "ACME", "RAXGBC").
        /// Stored upper-case.
        /// </summary>
        [BsonElement("code")]
        public string Code { get; set; } = string.Empty;

        /// <summary>Primary contact e-mail address for the client.</summary>
        [BsonElement("contactEmail")]
        [BsonIgnoreIfNull]
        public string? ContactEmail { get; set; }

        /// <summary>Primary contact phone number.</summary>
        [BsonElement("contactPhone")]
        [BsonIgnoreIfNull]
        public string? ContactPhone { get; set; }

        /// <summary>Physical address of the client.</summary>
        [BsonElement("address")]
        [BsonIgnoreIfNull]
        public string? Address { get; set; }

        /// <summary>Free-form notes about the client.</summary>
        [BsonElement("notes")]
        [BsonIgnoreIfNull]
        public string? Notes { get; set; }

        /// <summary>Whether the client is currently active on the platform.</summary>
        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("createdByUserId")]
        [BsonIgnoreIfNull]
        public string? CreatedByUserId { get; set; }
    }
}
