using System.ComponentModel.DataAnnotations;
using OTA.API.Models.Enums;

namespace OTA.API.Models.DTOs
{
    // ─────────────────────────────────────────────────────────────────────────
    // Firmware DTOs
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Request body for manually creating a firmware version entry.</summary>
    public sealed class CreateFirmwareRequest
    {
        /// <summary>RepositoryId the firmware originates from.</summary>
        [Required]
        [MaxLength(36)]
        public string RepositoryId { get; set; } = string.Empty;

        /// <summary>Semantic version string (e.g., "2.4.1").</summary>
        [Required]
        [MaxLength(50)]
        public string Version { get; set; } = string.Empty;

        /// <summary>Gitea release ID this firmware corresponds to.</summary>
        public long GiteaReleaseId { get; set; }

        /// <summary>Git tag name (e.g., "v2.4.1").</summary>
        [MaxLength(100)]
        public string? GiteaTagName { get; set; }

        /// <summary>Markdown release notes.</summary>
        [MaxLength(10000)]
        public string? ReleaseNotes { get; set; }

        /// <summary>Primary firmware binary file name (original upload name).</summary>
        [MaxLength(255)]
        public string? FileName { get; set; }

        /// <summary>
        /// Server-side stored file name returned by the upload endpoint (GUID-prefixed).
        /// When provided, the API will push the file to the Gitea repository under the version folder.
        /// </summary>
        [MaxLength(512)]
        public string? StoredFileName { get; set; }

        /// <summary>SHA-256 hex digest of the primary binary.</summary>
        [MaxLength(64)]
        public string? FileSha256 { get; set; }

        /// <summary>File size in bytes.</summary>
        public long FileSizeBytes { get; set; }

        /// <summary>Direct download URL for the firmware binary.</summary>
        [MaxLength(2048)]
        public string? DownloadUrl { get; set; }

        /// <summary>Distribution channel for this firmware.</summary>
        public FirmwareChannel Channel { get; set; } = FirmwareChannel.Alpha;

        /// <summary>Whether the update is mandatory for targeted devices.</summary>
        public bool IsMandate { get; set; } = false;

        /// <summary>List of compatible device model identifiers.</summary>
        public List<string> SupportedModels { get; set; } = new();

        /// <summary>List of compatible hardware revision identifiers.</summary>
        public List<string> SupportedHardwareRevisions { get; set; } = new();

        /// <summary>Minimum firmware version required on device before this update can be applied.</summary>
        [MaxLength(50)]
        public string? MinRequiredVersion { get; set; }

        /// <summary>Maximum current firmware version that may receive this update.</summary>
        [MaxLength(50)]
        public string? MaxAllowedVersion { get; set; }
    }

    /// <summary>Request body for updating mutable firmware metadata fields.</summary>
    public sealed class UpdateFirmwareRequest
    {
        /// <summary>Updated release notes.</summary>
        [MaxLength(10000)]
        public string? ReleaseNotes { get; set; }

        /// <summary>Updated mandatory flag.</summary>
        public bool? IsMandate { get; set; }

        /// <summary>Updated supported models list.</summary>
        public List<string>? SupportedModels { get; set; }

        /// <summary>Updated supported hardware revisions list.</summary>
        public List<string>? SupportedHardwareRevisions { get; set; }

        /// <summary>Updated minimum required version.</summary>
        [MaxLength(50)]
        public string? MinRequiredVersion { get; set; }

        /// <summary>Updated maximum allowed version.</summary>
        [MaxLength(50)]
        public string? MaxAllowedVersion { get; set; }
    }

    /// <summary>Request body for approving a firmware version (PendingApproval → Approved).</summary>
    public sealed class ApproveFirmwareRequest
    {
        /// <summary>Optional approval notes explaining the rationale for approval.</summary>
        [MaxLength(2000)]
        public string? ApprovalNotes { get; set; }
    }

    /// <summary>Request body for rejecting a firmware version at approval or QA stage.</summary>
    public sealed class RejectFirmwareRequest
    {
        /// <summary>Mandatory rejection reason; will be visible to the firmware submitter.</summary>
        [Required(ErrorMessage = "Rejection reason is required.")]
        [MaxLength(2000)]
        public string RejectionReason { get; set; } = string.Empty;
    }

    /// <summary>Request body for a QA engineer to mark a firmware version as QA verified.</summary>
    public sealed class QAVerifyFirmwareRequest
    {
        /// <summary>QA verification remarks summarising the testing performed.</summary>
        [MaxLength(2000)]
        public string? QaRemarks { get; set; }
    }

    /// <summary>Request body for assigning or changing a firmware version's distribution channel.</summary>
    public sealed class AssignChannelRequest
    {
        /// <summary>New distribution channel to assign.</summary>
        [Required]
        public FirmwareChannel Channel { get; set; }
    }

    /// <summary>Summary row for paginated firmware list responses.</summary>
    public sealed class FirmwareListDto
    {
        public string FirmwareId { get; set; } = string.Empty;
        public string RepositoryId { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string GiteaTagName { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsMandate { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }

    /// <summary>Full firmware detail returned by GET /api/firmware/{firmwareId}.</summary>
    public class FirmwareDetailDto
    {
        public string FirmwareId { get; set; } = string.Empty;

        /// <summary>Frontend-friendly alias for FirmwareId.</summary>
        public string Id => FirmwareId;

        public string RepositoryId { get; set; } = string.Empty;

        /// <summary>Display name of the linked Gitea repository (populated by service).</summary>
        public string? RepositoryName { get; set; }

        public string ProjectId { get; set; } = string.Empty;

        /// <summary>Display name of the parent project (populated by service).</summary>
        public string? ProjectName { get; set; }

        public string Version { get; set; } = string.Empty;
        public long GiteaReleaseId { get; set; }
        public string GiteaTagName { get; set; } = string.Empty;
        public string? ReleaseNotes { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileSha256 { get; set; } = string.Empty;

        /// <summary>Frontend-friendly alias for FileSha256.</summary>
        public string? Checksum => string.IsNullOrEmpty(FileSha256) ? null : FileSha256;

        public long FileSizeBytes { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsMandate { get; set; }
        public List<string> SupportedModels { get; set; } = new();
        public List<string> SupportedHardwareRevisions { get; set; } = new();
        public string? MinRequiredVersion { get; set; }
        public string? MaxAllowedVersion { get; set; }

        /// <summary>Current QA testing session status (from the linked QASession record). Null if no session started.</summary>
        public string? QaSessionStatus { get; set; }

        // ── QA fields ────────────────────────────────────────────────────────────
        public DateTime? QaVerifiedAt { get; set; }
        public string? QaVerifiedByUserId { get; set; }

        /// <summary>Display name of the user who QA-verified this firmware. Populated on single-fetch.</summary>
        public string? QaVerifiedByName { get; set; }

        /// <summary>Frontend-friendly alias — prefers enriched name, falls back to userId.</summary>
        public string? QaVerifiedBy => QaVerifiedByName ?? QaVerifiedByUserId;

        public string? QaRemarks { get; set; }

        /// <summary>True when QA verification has been completed.</summary>
        public bool IsQaVerified => QaVerifiedAt.HasValue;

        // ── Approval fields ───────────────────────────────────────────────────────
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedByUserId { get; set; }

        /// <summary>Display name of the approver. Populated on single-fetch.</summary>
        public string? ApprovedByName { get; set; }

        /// <summary>Frontend-friendly alias — prefers enriched name, falls back to userId.</summary>
        public string? ApprovedBy => ApprovedByName ?? ApprovedByUserId;

        public string? ApprovalNotes { get; set; }

        // ── Rejection fields ──────────────────────────────────────────────────────
        public DateTime? RejectedAt { get; set; }
        public string? RejectedByUserId { get; set; }

        /// <summary>Display name of the rejector. Populated on single-fetch.</summary>
        public string? RejectedByName { get; set; }

        /// <summary>Frontend-friendly alias — prefers enriched name, falls back to userId.</summary>
        public string? RejectedBy => RejectedByName ?? RejectedByUserId;

        public string? RejectionReason { get; set; }

        // ── Audit ─────────────────────────────────────────────────────────────────
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty;

        /// <summary>Enriched display name/email of the user who created this firmware. Populated on single-fetch.</summary>
        public string? CreatedByName { get; set; }

        public List<GiteaAssetItemDto> GiteaAssets { get; set; } = new();
    }

    /// <summary>Representation of a single Gitea release asset within firmware detail responses.</summary>
    public sealed class GiteaAssetItemDto
    {
        public long AssetId { get; set; }

        /// <summary>Frontend-friendly string alias for AssetId.</summary>
        public string Id => AssetId.ToString();

        public string Name { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public long SizeBytes { get; set; }

        /// <summary>Frontend-friendly alias for SizeBytes.</summary>
        public long Size => SizeBytes;

        public string ContentType { get; set; } = string.Empty;
    }

    /// <summary>Wrapper for paginated firmware list responses.</summary>
    public sealed class PagedFirmwareListResponse
    {
        public List<FirmwareListDto> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public long TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    /// <summary>Response returned after a successful firmware binary file upload.</summary>
    public sealed class UploadFirmwareFileResponse
    {
        /// <summary>Original filename as provided by the client.</summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>Server-side unique filename (GUID-prefixed) used for storage.</summary>
        public string StoredFileName { get; set; } = string.Empty;

        /// <summary>File size in bytes.</summary>
        public long FileSizeBytes { get; set; }

        /// <summary>Lowercase hex SHA-256 digest of the uploaded file.</summary>
        public string FileSha256 { get; set; } = string.Empty;

        /// <summary>Public URL from which the firmware binary can be downloaded.</summary>
        public string DownloadUrl { get; set; } = string.Empty;
    }
}
