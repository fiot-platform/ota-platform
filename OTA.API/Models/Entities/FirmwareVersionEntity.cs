using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using OTA.API.Models.Enums;

namespace OTA.API.Models.Entities
{
    /// <summary>
    /// Embedded sub-document representing a single binary asset attached to a Gitea release.
    /// </summary>
    public sealed class GiteaAssetDocument
    {
        /// <summary>Gitea's internal asset ID integer.</summary>
        [BsonElement("assetId")]
        public long AssetId { get; set; }

        /// <summary>File name of the asset (e.g., "firmware-edge-gateway-v2.4.1.bin").</summary>
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Direct download URL for this asset from the Gitea release.</summary>
        [BsonElement("downloadUrl")]
        public string DownloadUrl { get; set; } = string.Empty;

        /// <summary>Size of the asset in bytes.</summary>
        [BsonElement("sizeBytes")]
        public long SizeBytes { get; set; }

        /// <summary>MIME content type of the asset (e.g., "application/octet-stream").</summary>
        [BsonElement("contentType")]
        public string ContentType { get; set; } = "application/octet-stream";
    }

    /// <summary>
    /// MongoDB document representing a specific firmware version entry in the OTA platform catalogue.
    /// Each entry corresponds to one Gitea release and goes through a QA + approval workflow
    /// before it may be deployed to production devices.
    /// Collection: firmware_versions
    /// Indexes:
    ///   - Unique: FirmwareId
    ///   - Compound: {RepositoryId, Version} (unique)
    ///   - Single: ProjectId
    ///   - Single: Status
    ///   - Compound: {Channel, Status}
    ///   - Single: GiteaTagName
    ///   - Single: GiteaReleaseId
    /// </summary>
    public sealed class FirmwareVersionEntity
    {
        /// <summary>MongoDB internal ObjectId (_id).</summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        /// <summary>Platform-generated unique identifier for this firmware version (GUID string).</summary>
        [BsonElement("firmwareId")]
        public string FirmwareId { get; set; } = string.Empty;

        /// <summary>RepositoryId of the Gitea repository from which this firmware originates.</summary>
        [BsonElement("repositoryId")]
        public string RepositoryId { get; set; } = string.Empty;

        /// <summary>ProjectId of the parent project (denormalised for efficient scoped queries).</summary>
        [BsonElement("projectId")]
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>Semantic version string (e.g., "2.4.1" or "2.4.1-beta.3").</summary>
        [BsonElement("version")]
        public string Version { get; set; } = string.Empty;

        /// <summary>Gitea's internal integer ID for the release that produced this firmware.</summary>
        [BsonElement("giteaReleaseId")]
        public long GiteaReleaseId { get; set; }

        /// <summary>Git tag name associated with the Gitea release (e.g., "v2.4.1").</summary>
        [BsonElement("giteaTagName")]
        public string GiteaTagName { get; set; } = string.Empty;

        /// <summary>Release notes / changelog from the Gitea release body (Markdown supported).</summary>
        [BsonElement("releaseNotes")]
        [BsonIgnoreIfNull]
        public string? ReleaseNotes { get; set; }

        /// <summary>Primary firmware binary file name (e.g., "firmware-edge-gateway-v2.4.1.bin").</summary>
        [BsonElement("fileName")]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// SHA-256 hex digest of the primary firmware binary.
        /// Devices verify this checksum after download to ensure file integrity.
        /// </summary>
        [BsonElement("fileSha256")]
        public string FileSha256 { get; set; } = string.Empty;

        /// <summary>File size of the primary firmware binary in bytes.</summary>
        [BsonElement("fileSizeBytes")]
        public long FileSizeBytes { get; set; }

        /// <summary>Direct download URL for the primary firmware binary from Gitea.</summary>
        [BsonElement("downloadUrl")]
        public string DownloadUrl { get; set; } = string.Empty;

        /// <summary>Distribution channel to which this firmware is assigned.</summary>
        [BsonElement("channel")]
        [BsonRepresentation(BsonType.String)]
        public FirmwareChannel Channel { get; set; } = FirmwareChannel.Alpha;

        /// <summary>Current lifecycle status of this firmware version in the approval workflow.</summary>
        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public FirmwareStatus Status { get; set; } = FirmwareStatus.Draft;

        /// <summary>
        /// Whether this firmware update is mandatory.
        /// Mandatory updates must be applied by devices; they cannot defer the installation.
        /// </summary>
        [BsonElement("isMandate")]
        public bool IsMandate { get; set; } = false;

        /// <summary>
        /// List of device model identifiers that are compatible with this firmware
        /// (e.g., ["EDGE-GW-V1", "EDGE-GW-V2"]). Empty list means all models.
        /// </summary>
        [BsonElement("supportedModels")]
        public List<string> SupportedModels { get; set; } = new();

        /// <summary>
        /// List of hardware revision identifiers compatible with this firmware
        /// (e.g., ["REV-A", "REV-B"]). Empty list means all revisions.
        /// </summary>
        [BsonElement("supportedHardwareRevisions")]
        public List<string> SupportedHardwareRevisions { get; set; } = new();

        /// <summary>
        /// Minimum firmware version a device must currently have installed to receive this update.
        /// Used to enforce sequential upgrade paths. Null means no minimum constraint.
        /// </summary>
        [BsonElement("minRequiredVersion")]
        [BsonIgnoreIfNull]
        public string? MinRequiredVersion { get; set; }

        /// <summary>
        /// Maximum firmware version beyond which a device should not receive this update (downgrade guard).
        /// Null means no maximum constraint.
        /// </summary>
        [BsonElement("maxAllowedVersion")]
        [BsonIgnoreIfNull]
        public string? MaxAllowedVersion { get; set; }

        // ── QA Verification Fields ────────────────────────────────────────────

        /// <summary>UTC timestamp when a QA engineer marked this firmware as QAVerified.</summary>
        [BsonElement("qaVerifiedAt")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? QaVerifiedAt { get; set; }

        /// <summary>UserId of the QA engineer who performed the verification.</summary>
        [BsonElement("qaVerifiedByUserId")]
        [BsonIgnoreIfNull]
        public string? QaVerifiedByUserId { get; set; }

        /// <summary>Optional remarks entered by the QA engineer during verification.</summary>
        [BsonElement("qaRemarks")]
        [BsonIgnoreIfNull]
        public string? QaRemarks { get; set; }

        // ── Approval Fields ───────────────────────────────────────────────────

        /// <summary>UTC timestamp when the release manager approved this firmware for deployment.</summary>
        [BsonElement("approvedAt")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? ApprovedAt { get; set; }

        /// <summary>UserId of the release manager who approved this firmware.</summary>
        [BsonElement("approvedByUserId")]
        [BsonIgnoreIfNull]
        public string? ApprovedByUserId { get; set; }

        /// <summary>Optional notes entered by the approver during the approval action.</summary>
        [BsonElement("approvalNotes")]
        [BsonIgnoreIfNull]
        public string? ApprovalNotes { get; set; }

        /// <summary>UTC timestamp when this firmware was rejected (at QA or approval stage).</summary>
        [BsonElement("rejectedAt")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? RejectedAt { get; set; }

        /// <summary>UserId of the user who rejected this firmware.</summary>
        [BsonElement("rejectedByUserId")]
        [BsonIgnoreIfNull]
        public string? RejectedByUserId { get; set; }

        /// <summary>Mandatory rejection reason entered by the approver or QA engineer.</summary>
        [BsonElement("rejectionReason")]
        [BsonIgnoreIfNull]
        public string? RejectionReason { get; set; }

        // ── Audit Fields ──────────────────────────────────────────────────────

        /// <summary>UTC timestamp when this firmware entry was first created (via webhook or manually).</summary>
        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>UTC timestamp of the most recent update to this record.</summary>
        [BsonElement("updatedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>UserId of the platform user or system process that created this firmware record.</summary>
        [BsonElement("createdByUserId")]
        public string CreatedByUserId { get; set; } = string.Empty;

        /// <summary>Display name of the user who created this record (denormalised at creation time).</summary>
        [BsonElement("createdByName")]
        [BsonIgnoreIfNull]
        public string? CreatedByName { get; set; }

        // ── Assets ────────────────────────────────────────────────────────────

        /// <summary>
        /// All binary assets attached to the Gitea release (may include multiple files:
        /// main binary, symbols file, checksums file, etc.).
        /// </summary>
        [BsonElement("giteaAssets")]
        public List<GiteaAssetDocument> GiteaAssets { get; set; } = new();
    }
}
