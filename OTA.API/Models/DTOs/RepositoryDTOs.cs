using System.ComponentModel.DataAnnotations;

namespace OTA.API.Models.DTOs
{
    // ─────────────────────────────────────────────────────────────────────────
    // Repository DTOs
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Request body for registering a Gitea repository with the OTA platform.</summary>
    public sealed class RegisterRepositoryRequest
    {
        /// <summary>Short name of the Gitea repository (e.g., "firmware-edge-gateway").</summary>
        [Required(ErrorMessage = "GiteaRepoName is required.")]
        [MaxLength(200)]
        public string GiteaRepoName { get; set; } = string.Empty;

        /// <summary>Gitea owner (organisation slug or user login) of the repository.</summary>
        [Required(ErrorMessage = "GiteaOwner is required.")]
        [MaxLength(100)]
        public string GiteaOwner { get; set; } = string.Empty;

        /// <summary>ProjectId of the parent project to associate this repository with.</summary>
        [Required(ErrorMessage = "ProjectId is required.")]
        [MaxLength(36)]
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>Optional description of the repository's purpose.</summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>Default branch name (defaults to "main" if not specified).</summary>
        [MaxLength(100)]
        public string DefaultBranch { get; set; } = "main";

        /// <summary>When true the repository is created as private on Gitea. Ignored if the repo already exists.</summary>
        public bool IsPrivate { get; set; } = false;
    }

    /// <summary>Request body for updating an existing repository registration.</summary>
    public sealed class UpdateRepositoryRequest
    {
        /// <summary>Updated display name.</summary>
        [MaxLength(200)]
        public string? Name { get; set; }

        /// <summary>Updated description.</summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>Updated default branch name.</summary>
        [MaxLength(100)]
        public string? DefaultBranch { get; set; }

        /// <summary>Active / inactive state toggle.</summary>
        public bool? IsActive { get; set; }
    }

    /// <summary>Request body for triggering a manual Gitea sync for a repository.</summary>
    public sealed class SyncRepositoryRequest
    {
        /// <summary>
        /// If true, perform a full re-sync of all releases (not just new ones since last sync).
        /// Use sparingly — full syncs are more expensive.
        /// </summary>
        public bool FullSync { get; set; } = false;
    }

    /// <summary>Summary row for paginated repository list responses.</summary>
    public sealed class RepositoryListDto
    {
        public string RepositoryId { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string GiteaRepoName { get; set; } = string.Empty;
        public string GiteaOwner { get; set; } = string.Empty;
        public string GiteaCloneUrl { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? LastSyncedAt { get; set; }
        public int FirmwareVersionCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Full repository detail returned by GET /api/repositories/{repositoryId}.</summary>
    public class RepositoryDetailDto
    {
        public string Id { get; set; } = string.Empty;
        public string RepositoryId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        /// <summary>Alias for GiteaRepoName — matches the frontend Repository.giteaRepo field.</summary>
        public string GiteaRepo { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string? ProjectName { get; set; }
        public long GiteaRepoId { get; set; }
        public string GiteaRepoName { get; set; } = string.Empty;
        public string GiteaOwner { get; set; } = string.Empty;
        public string GiteaCloneUrl { get; set; } = string.Empty;
        public string? GiteaUrl { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public bool WebhookConfigured { get; set; }
        public DateTime? LastSyncedAt { get; set; }
        public string DefaultBranch { get; set; } = string.Empty;
        public List<string> Topics { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty;
        public int FirmwareVersionCount { get; set; }
    }

    /// <summary>
    /// DTO representing a repository record as returned by the Gitea REST API.
    /// Used for repository discovery and sync operations.
    /// </summary>
    public sealed class GiteaRepoDto
    {
        /// <summary>Gitea internal repository ID.</summary>
        public long Id { get; set; }

        /// <summary>Short repository name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Full repository path including owner (e.g., "acme-iot/firmware-edge-gateway").</summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>Repository description from Gitea.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>HTTPS clone URL.</summary>
        public string CloneUrl { get; set; } = string.Empty;

        /// <summary>Default branch name.</summary>
        public string DefaultBranch { get; set; } = "main";

        /// <summary>Repository topics/tags from Gitea.</summary>
        public List<string> Topics { get; set; } = new();

        /// <summary>Whether the repository is archived in Gitea.</summary>
        public bool Archived { get; set; }

        /// <summary>UTC timestamp of the repository's last push event in Gitea.</summary>
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>Wrapper for paginated repository list responses.</summary>
    public sealed class PagedRepositoryListResponse
    {
        public List<RepositoryListDto> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public long TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
