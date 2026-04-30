namespace OTA.API.Models.Settings
{
    /// <summary>
    /// Strongly-typed settings for the MongoDB connection and collection name map.
    /// Bind from appsettings.json section "MongoDbSettings".
    /// </summary>
    public sealed class MongoDbSettings
    {
        public const string SectionName = "MongoDbSettings";
        /// <summary>Full MongoDB connection string including credentials and replica set options.</summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>Target database name within the MongoDB cluster.</summary>
        public string DatabaseName { get; set; } = string.Empty;

        // ── Collection Names ──────────────────────────────────────────────────

        /// <summary>Collection storing platform user accounts.</summary>
        public string UsersCollection { get; set; } = "users";

        /// <summary>Collection storing customer project definitions.</summary>
        public string ProjectsCollection { get; set; } = "projects";

        /// <summary>Collection storing Gitea repository registrations.</summary>
        public string RepositoriesCollection { get; set; } = "repositories";

        /// <summary>Collection storing firmware version catalogue entries.</summary>
        public string FirmwareVersionsCollection { get; set; } = "firmware_versions";

        /// <summary>Collection storing registered IoT device records.</summary>
        public string DevicesCollection { get; set; } = "devices";

        /// <summary>Collection storing individual OTA update job records (one per device per rollout).</summary>
        public string OtaJobsCollection { get; set; } = "ota_jobs";

        /// <summary>Collection storing OTA rollout campaign records.</summary>
        public string RolloutsCollection { get; set; } = "rollouts";

        /// <summary>Collection storing inbound Gitea webhook event payloads and processing state.</summary>
        public string RepositoryEventsCollection { get; set; } = "repository_events";

        /// <summary>Collection storing immutable audit log entries.</summary>
        public string AuditLogsCollection { get; set; } = "audit_logs";

        /// <summary>Collection storing rollout policy definitions.</summary>
        public string RolloutPoliciesCollection { get; set; } = "rollout_policies";

        /// <summary>Collection storing client (customer organisation) records.</summary>
        public string ClientsCollection { get; set; } = "clients";
    }

    /// <summary>
    /// Strongly-typed settings for JWT token issuance and validation.
    /// Bind from appsettings.json section "JwtSettings".
    /// </summary>
    public sealed class JwtSettings
    {
        public const string SectionName = "JwtSettings";
        /// <summary>
        /// HMAC-SHA256 signing secret. Must be at least 32 characters in production.
        /// Store in a secrets manager (Azure Key Vault, AWS Secrets Manager, Vault) — never commit to source control.
        /// </summary>
        public string Secret { get; set; } = string.Empty;

        /// <summary>Token issuer claim — typically the canonical API base URL.</summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>Token audience claim — typically the frontend application URL or identifier.</summary>
        public string Audience { get; set; } = string.Empty;

        /// <summary>Lifetime of the access token in minutes. Default: 15 minutes.</summary>
        public int ExpiryMinutes { get; set; } = 15;

        /// <summary>Lifetime of the refresh token in days. Default: 7 days.</summary>
        public int RefreshTokenExpiryDays { get; set; } = 7;
    }

    /// <summary>
    /// Strongly-typed settings for the Gitea integration client.
    /// Bind from appsettings.json section "GiteaSettings".
    /// </summary>
    public sealed class GiteaSettings
    {
        public const string SectionName = "GiteaSettings";
        /// <summary>Base URL of the Gitea instance (e.g., https://gitea.internal.example.com).</summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gitea admin API token used by the OTA API to query repositories, releases, and tags.
        /// Must have repository read access at minimum. Store in secrets manager.
        /// </summary>
        public string AdminToken { get; set; } = string.Empty;

        /// <summary>
        /// HMAC-SHA256 shared secret configured on each Gitea repository webhook.
        /// Used to validate the X-Gitea-Signature-256 header on inbound webhook requests.
        /// </summary>
        public string WebhookSecret { get; set; } = string.Empty;

        /// <summary>HTTP request timeout in seconds for outbound Gitea API calls. Default: 30 seconds.</summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>Maximum number of retries for transient Gitea API failures using exponential back-off.</summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>Base URL path prefix for the Gitea API (e.g., /api/v1). Default: /api/v1.</summary>
        public string ApiPathPrefix { get; set; } = "/api/v1";
    }

    /// <summary>
    /// Root application settings class. All sub-sections are composed here for
    /// dependency-injection via IOptions&lt;AppSettings&gt;.
    /// Bind from appsettings.json section "AppSettings".
    /// </summary>
    public sealed class AppSettings
    {
        /// <summary>MongoDB connection and collection configuration.</summary>
        public MongoDbSettings MongoDb { get; set; } = new();

        /// <summary>JWT token issuance and validation configuration.</summary>
        public JwtSettings Jwt { get; set; } = new();

        /// <summary>Gitea integration client configuration.</summary>
        public Gitea GiteaSettings { get; set; } = new();

        /// <summary>General application-level settings.</summary>
        public GeneralSettings General { get; set; } = new();
    }

    /// <summary>
    /// Nested Gitea settings within AppSettings for clean binding.
    /// </summary>
    public sealed class Gitea
    {
        /// <summary>Base URL of the Gitea instance.</summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>Gitea admin API token.</summary>
        public string AdminToken { get; set; } = string.Empty;

        /// <summary>Shared HMAC secret for webhook signature validation.</summary>
        public string WebhookSecret { get; set; } = string.Empty;

        /// <summary>HTTP request timeout in seconds.</summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>Maximum retry attempts for transient failures.</summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>Gitea REST API path prefix.</summary>
        public string ApiPathPrefix { get; set; } = "/api/v1";
    }

    /// <summary>
    /// General platform operational settings.
    /// </summary>
    public sealed class GeneralSettings
    {
        /// <summary>Maximum number of devices that can be targeted in a single rollout.</summary>
        public int MaxDevicesPerRollout { get; set; } = 10000;

        /// <summary>Interval in seconds for the background rollout monitor polling loop.</summary>
        public int RolloutMonitorIntervalSeconds { get; set; } = 30;

        /// <summary>Number of hours after which an inactive device is flagged as Inactive.</summary>
        public int DeviceInactivityThresholdHours { get; set; } = 24;

        /// <summary>Maximum number of webhook retry attempts before marking an event as Failed.</summary>
        public int WebhookMaxRetryAttempts { get; set; } = 5;

        /// <summary>Base interval in seconds for webhook retry back-off (doubles each attempt).</summary>
        public int WebhookRetryBaseIntervalSeconds { get; set; } = 60;

        /// <summary>Default page size for paginated list endpoints.</summary>
        public int DefaultPageSize { get; set; } = 25;

        /// <summary>Maximum allowed page size for paginated list endpoints.</summary>
        public int MaxPageSize { get; set; } = 200;
    }
}
