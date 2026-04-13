using System.Text.Json.Serialization;

namespace OTA.API.Models.DTOs
{
    // ─────────────────────────────────────────────────────────────────────────
    // Gitea API DTOs
    // These DTOs map to responses from the Gitea REST API (api/v1).
    // JsonPropertyName attributes match Gitea's snake_case JSON field names.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Gitea user object embedded in release and repository responses.</summary>
    public sealed class GiteaUserDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;

        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("avatar_url")]
        public string AvatarUrl { get; set; } = string.Empty;
    }

    /// <summary>Gitea repository object as returned by /api/v1/repos/{owner}/{repo}.</summary>
    public sealed class GiteaRepositoryDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("clone_url")]
        public string CloneUrl { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("ssh_url")]
        public string SshUrl { get; set; } = string.Empty;

        [JsonPropertyName("default_branch")]
        public string DefaultBranch { get; set; } = "main";

        [JsonPropertyName("archived")]
        public bool Archived { get; set; }

        [JsonPropertyName("private")]
        public bool Private { get; set; }

        [JsonPropertyName("topics")]
        public List<string> Topics { get; set; } = new();

        [JsonPropertyName("owner")]
        public GiteaUserDto Owner { get; set; } = new();

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Single asset file attached to a Gitea release.</summary>
    public sealed class GiteaAssetDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("download_count")]
        public long DownloadCount { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("uuid")]
        public string Uuid { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }

    /// <summary>Gitea release object as returned by /api/v1/repos/{owner}/{repo}/releases/{id}.</summary>
    public sealed class GiteaReleaseDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        [JsonPropertyName("draft")]
        public bool Draft { get; set; }

        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("published_at")]
        public DateTime PublishedAt { get; set; }

        [JsonPropertyName("author")]
        public GiteaUserDto Author { get; set; } = new();

        [JsonPropertyName("assets")]
        public List<GiteaAssetDto> Assets { get; set; } = new();

        [JsonPropertyName("target_commitish")]
        public string TargetCommitish { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    /// <summary>Gitea lightweight tag object as returned by /api/v1/repos/{owner}/{repo}/tags.</summary>
    public sealed class GiteaTagDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("tarball_url")]
        public string TarballUrl { get; set; } = string.Empty;

        [JsonPropertyName("zipball_url")]
        public string ZipballUrl { get; set; } = string.Empty;

        [JsonPropertyName("commit")]
        public GiteaTagCommitDto Commit { get; set; } = new();
    }

    /// <summary>Commit summary nested inside a Gitea tag response.</summary>
    public sealed class GiteaTagCommitDto
    {
        [JsonPropertyName("sha")]
        public string Sha { get; set; } = string.Empty;

        [JsonPropertyName("created")]
        public DateTime Created { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    /// <summary>
    /// Full Gitea webhook payload DTO. Fields are populated from the JSON body
    /// of inbound webhook POST requests. Only the fields consumed by the OTA platform
    /// are mapped here; the full raw payload is also stored verbatim.
    /// </summary>
    public sealed class GiteaWebhookPayloadDto
    {
        /// <summary>The X-Gitea-Event header value (e.g., "release", "push", "create").</summary>
        [JsonPropertyName("secret")]
        public string? Secret { get; set; }

        [JsonPropertyName("ref")]
        public string? Ref { get; set; }

        [JsonPropertyName("before")]
        public string? Before { get; set; }

        [JsonPropertyName("after")]
        public string? After { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("release")]
        public GiteaReleaseDto? Release { get; set; }

        [JsonPropertyName("repository")]
        public GiteaRepositoryDto? Repository { get; set; }

        [JsonPropertyName("sender")]
        public GiteaUserDto? Sender { get; set; }

        [JsonPropertyName("commits")]
        public List<GiteaCommitDto> Commits { get; set; } = new();
    }

    /// <summary>Minimal commit summary nested in a push webhook payload.</summary>
    public sealed class GiteaCommitDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("author")]
        public GiteaCommitAuthorDto Author { get; set; } = new();

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
    }

    /// <summary>Author information embedded in a Gitea commit.</summary>
    public sealed class GiteaCommitAuthorDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;
    }

    /// <summary>Paginated list of Gitea releases as returned by the list-releases API endpoint.</summary>
    public sealed class GiteaReleaseListResponse
    {
        /// <summary>The page of releases returned from this response.</summary>
        public List<GiteaReleaseDto> Releases { get; set; } = new();

        /// <summary>Total number of releases available (from X-Total-Count response header).</summary>
        public long TotalCount { get; set; }

        /// <summary>Page number that was requested.</summary>
        public int Page { get; set; }

        /// <summary>Page size used in the request.</summary>
        public int Limit { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Webhook Event DTOs
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Summary DTO for a webhook event as returned by the webhook event list endpoint.</summary>
    public sealed class RepositoryEventDto
    {
        /// <summary>MongoDB ObjectId string (_id).</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Platform-generated event identifier (GUID).</summary>
        public string EventId { get; set; } = string.Empty;

        public string DeliveryId { get; set; } = string.Empty;

        /// <summary>Gitea event type (e.g. "release", "push").</summary>
        public object? EventType { get; set; }

        public string? RepositoryId { get; set; }

        /// <summary>Gitea repository identifier (numeric).</summary>
        public string? GiteaRepoId { get; set; }

        public object? Status { get; set; }
        public int RetryCount { get; set; }

        /// <summary>Error message from the most recent failed attempt.</summary>
        public string? ErrorMessage { get; set; }

        public DateTime? ProcessedAt { get; set; }
        public DateTime ReceivedAt { get; set; }
    }

    /// <summary>Filter parameters for querying webhook events.</summary>
    public sealed class RepositoryEventFilterRequest
    {
        public string? RepositoryId { get; set; }
        public string? GiteaRepoId { get; set; }
        public string? Status { get; set; }
        public string? EventType { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;

        /// <summary>Maximum number of recent events to return when no specific filter is applied.</summary>
        public int Limit { get; set; } = 20;
    }
}
