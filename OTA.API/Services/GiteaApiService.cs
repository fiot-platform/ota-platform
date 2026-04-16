using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using OTA.API.Models.DTOs;
using OTA.API.Models.Settings;
using OTA.API.Repositories.Interfaces;
using OTA.API.Services.Interfaces;

namespace OTA.API.Services
{
    /// <summary>
    /// HTTP client implementation of <see cref="IGiteaApiService"/>.
    /// Uses Bearer token authentication, JSON deserialization, and exponential-backoff retry (3 attempts).
    /// All settings are read from <see cref="GiteaSettings"/> via <see cref="IOptions{T}"/>.
    /// </summary>
    public class GiteaApiService : IGiteaApiService
    {
        private readonly HttpClient _httpClient;
        private readonly GiteaSettings _settings;
        private readonly IRepositoryMasterRepository _repoRepository;
        private readonly ILogger<GiteaApiService> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private const int MaxRetries = 3;

        /// <summary>Initialises a new instance of <see cref="GiteaApiService"/>.</summary>
        public GiteaApiService(
            HttpClient httpClient,
            IOptions<GiteaSettings> settings,
            IRepositoryMasterRepository repoRepository,
            ILogger<GiteaApiService> logger)
        {
            _httpClient     = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _settings       = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _repoRepository = repoRepository ?? throw new ArgumentNullException(nameof(repoRepository));
            _logger         = logger ?? throw new ArgumentNullException(nameof(logger));

            ConfigureHttpClient();
        }

        private void ConfigureHttpClient()
        {
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/api/v1/");
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.AdminToken);
            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds > 0 ? _settings.TimeoutSeconds : 30);
        }

        /// <inheritdoc/>
        public async Task<GiteaRepositoryDto> GetRepositoryAsync(string owner, string repo, CancellationToken cancellationToken = default)
        {
            ValidateOwnerRepo(owner, repo);
            var response = await GetWithRetryAsync<GiteaRepositoryDto>($"repos/{owner}/{repo}", cancellationToken);
            return response ?? throw new InvalidOperationException($"Repository '{owner}/{repo}' not found on Gitea.");
        }

        /// <inheritdoc/>
        public async Task<List<GiteaReleaseDto>> GetReleasesAsync(string owner, string repo, CancellationToken cancellationToken = default)
        {
            ValidateOwnerRepo(owner, repo);
            return await GetWithRetryAsync<List<GiteaReleaseDto>>(
                $"repos/{owner}/{repo}/releases?limit=50", cancellationToken)
                ?? new List<GiteaReleaseDto>();
        }

        /// <inheritdoc/>
        public async Task<GiteaReleaseDto> GetReleaseByTagAsync(string owner, string repo, string tag, CancellationToken cancellationToken = default)
        {
            ValidateOwnerRepo(owner, repo);
            if (string.IsNullOrWhiteSpace(tag)) throw new ArgumentException("Tag is required.", nameof(tag));
            var response = await GetWithRetryAsync<GiteaReleaseDto>(
                $"repos/{owner}/{repo}/releases/tags/{Uri.EscapeDataString(tag)}", cancellationToken);
            return response ?? throw new InvalidOperationException($"Release tag '{tag}' not found in '{owner}/{repo}'.");
        }

        /// <inheritdoc/>
        public async Task<List<GiteaTagDto>> GetTagsAsync(string owner, string repo, CancellationToken cancellationToken = default)
        {
            ValidateOwnerRepo(owner, repo);
            return await GetWithRetryAsync<List<GiteaTagDto>>(
                $"repos/{owner}/{repo}/tags?limit=50", cancellationToken)
                ?? new List<GiteaTagDto>();
        }

        /// <inheritdoc/>
        public async Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var request  = new HttpRequestMessage(HttpMethod.Get, "user");
                var response = await _httpClient.SendAsync(request, cancellationToken);
                var isValid  = response.StatusCode == HttpStatusCode.OK;
                _logger.LogInformation("Gitea connection validation: {Status}.",
                    isValid ? "OK" : $"Failed ({response.StatusCode})");
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gitea connection validation failed with exception.");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task SyncRepositoryMetadataAsync(string repositoryId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(repositoryId))
                throw new ArgumentException("RepositoryId is required.", nameof(repositoryId));

            var entity = await _repoRepository.GetByIdAsync(repositoryId, cancellationToken)
                ?? throw new KeyNotFoundException($"Repository '{repositoryId}' not found.");

            var giteaRepo = await GetRepositoryAsync(entity.GiteaOwner, entity.GiteaRepoName, cancellationToken);

            entity.Description    = giteaRepo.Description ?? entity.Description;
            entity.DefaultBranch  = giteaRepo.DefaultBranch ?? entity.DefaultBranch;
            entity.GiteaUrl       = giteaRepo.HtmlUrl ?? entity.GiteaUrl;
            entity.LastSyncedAt   = DateTime.UtcNow;
            entity.UpdatedAt      = DateTime.UtcNow;

            await _repoRepository.UpdateAsync(repositoryId, entity, cancellationToken);
            _logger.LogInformation("Synced metadata for repository '{RepositoryId}' from Gitea.", repositoryId);
        }

        /// <inheritdoc/>
        public async Task<GiteaUserDto?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;

            try
            {
                // Strategy 1: keyword search (works when Gitea login/name contains the email string)
                var searchResult = await GetWithRetryAsync<GiteaUserSearchResult>(
                    $"users/search?q={Uri.EscapeDataString(email)}&limit=10", cancellationToken);

                var found = searchResult?.Data?.FirstOrDefault(u =>
                    string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

                if (found != null) return found;

                // Strategy 2: admin listing — returns full user objects with emails visible.
                // Necessary when the Gitea login name doesn't contain the email address.
                // Paginate up to 200 users; for larger installs a dedicated email-search
                // API would be needed.
                for (int page = 1; page <= 4; page++)
                {
                    var users = await GetWithRetryAsync<List<GiteaUserDto>>(
                        $"admin/users?limit=50&page={page}", cancellationToken);

                    if (users == null || users.Count == 0) break;

                    var match = users.FirstOrDefault(u =>
                        string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

                    if (match != null) return match;
                    if (users.Count < 50) break; // Last page reached
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gitea user search for email '{Email}' failed — treating as not found.", email);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> VerifyUserCredentialsAsync(string usernameOrEmail, string password, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
                return false;

            try
            {
                var credentials = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes($"{usernameOrEmail}:{password}"));

                using var request = new HttpRequestMessage(HttpMethod.Get, "user");
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

                // Use a short timeout — don't hold up login for a slow Gitea
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(5));

                using var response = await _httpClient.SendAsync(request, cts.Token);
                return response.StatusCode == HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gitea credential verification failed — treating as invalid.");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<GiteaRepositoryDto> CreateRepositoryForUserAsync(
            string owner,
            string repoName,
            string? description,
            string defaultBranch,
            bool isPrivate,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(owner))   throw new ArgumentException("Owner is required.",   nameof(owner));
            if (string.IsNullOrWhiteSpace(repoName)) throw new ArgumentException("RepoName is required.", nameof(repoName));

            var body = new
            {
                name           = repoName,
                description    = description ?? string.Empty,
                default_branch = string.IsNullOrWhiteSpace(defaultBranch) ? "main" : defaultBranch,
                @private       = isPrivate,
                auto_init      = true
            };

            var json    = System.Text.Json.JsonSerializer.Serialize(body);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            using var request  = new HttpRequestMessage(HttpMethod.Post,
                $"admin/users/{Uri.EscapeDataString(owner)}/repos")
            {
                Content = content
            };

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException(
                    $"Gitea rejected repo creation for '{owner}/{repoName}': HTTP {(int)response.StatusCode} — {errorBody}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return System.Text.Json.JsonSerializer.Deserialize<GiteaRepositoryDto>(responseJson, _jsonOptions)
                ?? throw new InvalidOperationException("Gitea returned an empty response when creating repository.");
        }

        /// <inheritdoc/>
        public async Task CreateFileAsync(
            string owner,
            string repo,
            string filePath,
            string commitMessage,
            byte[] content,
            string branch = "main",
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(owner))   throw new ArgumentException("Owner is required.",    nameof(owner));
            if (string.IsNullOrWhiteSpace(repo))     throw new ArgumentException("Repo is required.",     nameof(repo));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("FilePath is required.", nameof(filePath));

            // Gitea Contents API: PUT /api/v1/repos/{owner}/{repo}/contents/{filepath}
            var encodedPath = string.Join("/", filePath.Split('/').Select(Uri.EscapeDataString));
            var url = $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/contents/{encodedPath}";

            var body = System.Text.Json.JsonSerializer.Serialize(new
            {
                message = commitMessage,
                content = Convert.ToBase64String(content),
                branch  = string.IsNullOrWhiteSpace(branch) ? "main" : branch,
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
            };

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            // 201 Created = success; 422 Unprocessable = file already exists — both are acceptable
            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
            {
                _logger.LogDebug("Gitea file created: {Owner}/{Repo}/{Path}", owner, repo, filePath);
                return;
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Gitea CreateFile failed for {Owner}/{Repo}/{Path}: HTTP {Code} — {Error}",
                owner, repo, filePath, (int)response.StatusCode, error);
        }

        // ── Private helpers ─────────────────────────────────────────────────────────

        /// <summary>Wrapper for the Gitea user search response envelope.</summary>
        private sealed class GiteaUserSearchResult
        {
            [System.Text.Json.Serialization.JsonPropertyName("data")]
            public List<GiteaUserDto> Data { get; set; } = new();
        }

        private async Task<T?> GetWithRetryAsync<T>(string relativeUrl, CancellationToken cancellationToken) where T : class
        {
            Exception? lastException = null;

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    using var response = await _httpClient.GetAsync(relativeUrl, cancellationToken);

                    if (response.StatusCode == HttpStatusCode.NotFound)
                        return null;

                    if (!response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync(cancellationToken);
                        throw new HttpRequestException(
                            $"Gitea API returned {(int)response.StatusCode} for '{relativeUrl}'. Body: {body}");
                    }

                    var json = await response.Content.ReadAsStringAsync(cancellationToken);
                    return JsonSerializer.Deserialize<T>(json, _jsonOptions);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning(ex,
                        "Gitea API call to '{Url}' failed (attempt {Attempt}/{Max}).",
                        relativeUrl, attempt, MaxRetries);

                    if (attempt < MaxRetries)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 2s, 4s
                        await Task.Delay(delay, cancellationToken);
                    }
                }
            }

            throw new InvalidOperationException(
                $"Gitea API call to '{relativeUrl}' failed after {MaxRetries} attempts.", lastException);
        }

        private static void ValidateOwnerRepo(string owner, string repo)
        {
            if (string.IsNullOrWhiteSpace(owner)) throw new ArgumentException("Owner is required.", nameof(owner));
            if (string.IsNullOrWhiteSpace(repo))  throw new ArgumentException("Repo is required.",  nameof(repo));
        }
    }
}
