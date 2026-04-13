using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using OTA.API.Models.Enums;

namespace OTA.API.Models.Entities
{
    /// <summary>
    /// MongoDB document representing a platform user account.
    /// Collection: users
    /// Indexes:
    ///   - Unique: Email
    ///   - Single: UserId
    ///   - Single: CustomerId
    ///   - Single: Role
    ///   - TTL on RefreshTokenExpiry (optional, for automatic token cleanup)
    /// </summary>
    public sealed class UserEntity
    {
        /// <summary>MongoDB internal ObjectId (_id).</summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Platform-generated unique identifier for the user (GUID string).
        /// Used in all API responses and foreign-key references throughout the platform.
        /// </summary>
        [BsonElement("userId")]
        public string UserId { get; set; } = string.Empty;

        /// <summary>Full display name of the user (first name + last name).</summary>
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Unique email address; used as the login identifier.</summary>
        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// BCrypt-hashed password. Plain-text passwords are never persisted.
        /// Use BCrypt.Net-Next with work factor ≥ 12.
        /// </summary>
        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Assigned platform role governing RBAC permissions for this user.
        /// Stored as integer; deserialized to <see cref="UserRole"/> enum.
        /// </summary>
        [BsonElement("role")]
        [BsonRepresentation(BsonType.String)]
        public UserRole Role { get; set; }

        /// <summary>
        /// Customer tenant identifier. Null for platform-level roles (SuperAdmin, PlatformAdmin).
        /// Used to scope data access for CustomerAdmin and below.
        /// </summary>
        [BsonElement("customerId")]
        [BsonIgnoreIfNull]
        public string? CustomerId { get; set; }

        /// <summary>
        /// List of ProjectId values this user is explicitly scoped to.
        /// Empty list means the user has no project restrictions within their customer scope.
        /// Only applicable when role is ReleaseManager, QA, DevOpsEngineer, SupportEngineer, or Viewer.
        /// </summary>
        [BsonElement("projectScope")]
        public List<string> ProjectScope { get; set; } = new();

        /// <summary>
        /// Whether the user account is active and allowed to authenticate.
        /// Deactivated users retain their records for audit purposes.
        /// </summary>
        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        /// <summary>UTC timestamp when the user account was created.</summary>
        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>UTC timestamp of the most recent update to this user record.</summary>
        [BsonElement("updatedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>UTC timestamp of the user's most recent successful authentication.</summary>
        [BsonElement("lastLoginAt")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// Opaque refresh token value (stored as a SHA-256 hash of the raw token).
        /// The raw token is returned to the client at login and never re-stored plain-text.
        /// </summary>
        [BsonElement("refreshToken")]
        [BsonIgnoreIfNull]
        public string? RefreshToken { get; set; }

        /// <summary>UTC expiry timestamp of the current refresh token.</summary>
        [BsonElement("refreshTokenExpiry")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? RefreshTokenExpiry { get; set; }

        /// <summary>Alias for <see cref="RefreshTokenExpiry"/> used by AuthService and UserRepository.</summary>
        [BsonIgnore]
        public DateTime? RefreshTokenExpiresAt
        {
            get => RefreshTokenExpiry;
            set => RefreshTokenExpiry = value;
        }

        /// <summary>Alias for <see cref="Name"/> — full display name of the user.</summary>
        [BsonIgnore]
        public string FullName
        {
            get => Name;
            set => Name = value;
        }

        /// <summary>Optional phone number (not persisted unless added to schema).</summary>
        [BsonIgnore]
        public string? PhoneNumber { get; set; }
    }
}
