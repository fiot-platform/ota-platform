using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OTA.API.Models.DTOs;

namespace OTA.API.Services.Interfaces
{
    /// <summary>
    /// Service interface for communication with the Gitea REST API.
    /// Implementations use HttpClient with retry logic and Bearer token authentication.
    /// </summary>
    public interface IGiteaApiService
    {
        /// <summary>
        /// Retrieves repository metadata from the Gitea API for the given owner and repository name.
        /// </summary>
        /// <param name="owner">The Gitea organisation or user name that owns the repository.</param>
        /// <param name="repo">The repository name within the owner's namespace.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="GiteaRepositoryDto"/> containing repository metadata.</returns>
        Task<GiteaRepositoryDto> GetRepositoryAsync(string owner, string repo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all releases published for the specified Gitea repository.
        /// </summary>
        /// <param name="owner">The Gitea organisation or user name.</param>
        /// <param name="repo">The repository name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of <see cref="GiteaReleaseDto"/> objects representing each published release.</returns>
        Task<List<GiteaReleaseDto>> GetReleasesAsync(string owner, string repo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a single release from the Gitea API by its tag name.
        /// </summary>
        /// <param name="owner">The Gitea organisation or user name.</param>
        /// <param name="repo">The repository name.</param>
        /// <param name="tag">The tag name identifying the release (e.g. "v1.2.3").</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The <see cref="GiteaReleaseDto"/> for the specified tag.</returns>
        Task<GiteaReleaseDto> GetReleaseByTagAsync(string owner, string repo, string tag, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all git tags from the specified Gitea repository.
        /// </summary>
        /// <param name="owner">The Gitea organisation or user name.</param>
        /// <param name="repo">The repository name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of <see cref="GiteaTagDto"/> objects representing each tag.</returns>
        Task<List<GiteaTagDto>> GetTagsAsync(string owner, string repo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates that the configured Gitea server is reachable and the API token is valid.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the connection is valid; false if the server is unreachable or the token is rejected.</returns>
        Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Fetches fresh metadata from Gitea for the specified internal repository and persists the updates.
        /// </summary>
        /// <param name="repositoryId">The internal repository master identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SyncRepositoryMetadataAsync(string repositoryId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches the Gitea user directory for an account whose email matches the given address.
        /// Uses the configured admin token. Returns null when Gitea is unreachable or no match is found.
        /// </summary>
        Task<GiteaUserDto?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies a Gitea username/password pair by calling GET /api/v1/user with HTTP Basic auth.
        /// Returns true only when Gitea responds with 200 OK.
        /// Returns false (never throws) when Gitea is unreachable or credentials are wrong.
        /// </summary>
        Task<bool> VerifyUserCredentialsAsync(string usernameOrEmail, string password, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new repository on Gitea for the specified owner using the admin token.
        /// Throws <see cref="HttpRequestException"/> if Gitea rejects the request.
        /// </summary>
        /// <param name="owner">Gitea username or organisation that will own the repository.</param>
        /// <param name="repoName">Repository name (slug).</param>
        /// <param name="description">Optional description.</param>
        /// <param name="defaultBranch">Default branch name (e.g. "main").</param>
        /// <param name="isPrivate">Whether the repository should be private.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<GiteaRepositoryDto> CreateRepositoryForUserAsync(
            string owner,
            string repoName,
            string? description,
            string defaultBranch,
            bool isPrivate,
            CancellationToken cancellationToken = default);
    }
}
