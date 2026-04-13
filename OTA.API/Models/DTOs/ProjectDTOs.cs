using System.ComponentModel.DataAnnotations;

namespace OTA.API.Models.DTOs
{
    // ─────────────────────────────────────────────────────────────────────────
    // Project DTOs
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Request body for creating a new project.</summary>
    public sealed class CreateProjectRequest
    {
        /// <summary>Human-readable project name; must be unique within the customer tenant.</summary>
        [Required(ErrorMessage = "Project name is required.")]
        [MinLength(2)]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Optional description of the project's purpose and target device family.</summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>Customer tenant identifier that will own this project.</summary>
        [Required(ErrorMessage = "CustomerId is required.")]
        [MaxLength(36)]
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>Display name of the customer (used for denormalisation).</summary>
        [Required(ErrorMessage = "CustomerName is required.")]
        [MaxLength(200)]
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>Business unit or division responsible for this project.</summary>
        [MaxLength(200)]
        public string? BusinessUnit { get; set; }

        /// <summary>Gitea organisation slug under which this project's repositories are hosted.</summary>
        [MaxLength(100)]
        public string? GiteaOrgName { get; set; }

        /// <summary>Categorisation tags (e.g., ["production", "eu-region"]).</summary>
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>Request body for updating an existing project.</summary>
    public sealed class UpdateProjectRequest
    {
        /// <summary>Updated project name.</summary>
        [MinLength(2)]
        [MaxLength(200)]
        public string? Name { get; set; }

        /// <summary>Updated description.</summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>Updated business unit.</summary>
        [MaxLength(200)]
        public string? BusinessUnit { get; set; }

        /// <summary>Updated Gitea organisation name.</summary>
        [MaxLength(100)]
        public string? GiteaOrgName { get; set; }

        /// <summary>Updated tags list (replaces existing tags).</summary>
        public List<string>? Tags { get; set; }

        /// <summary>Active / inactive state toggle.</summary>
        public bool? IsActive { get; set; }
    }

    /// <summary>Summary row for paginated project list responses.</summary>
    public sealed class ProjectListDto
    {
        public string ProjectId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? BusinessUnit { get; set; }
        public bool IsActive { get; set; }
        public int RepositoryCount { get; set; }
        public int ActiveRolloutCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Full project detail returned by GET /api/projects/{projectId}.</summary>
    public class ProjectDetailDto
    {
        public string Id { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? BusinessUnit { get; set; }
        public bool IsActive { get; set; }
        public string? GiteaOrgName { get; set; }
        public List<string> Tags { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty;
        public int RepositoryCount { get; set; }
        public int FirmwareCount { get; set; }
        public int DeviceCount { get; set; }
        public int ActiveRolloutCount { get; set; }
    }

    /// <summary>Wrapper for paginated project list responses.</summary>
    public sealed class PagedProjectListResponse
    {
        public List<ProjectListDto> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public long TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
