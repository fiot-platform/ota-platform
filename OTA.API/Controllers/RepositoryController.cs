using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OTA.API.Helpers;
using OTA.API.Models.DTOs;
using OTA.API.Services.Interfaces;

namespace OTA.API.Controllers
{
    /// <summary>
    /// Repository management endpoints for registering, updating, syncing, and deactivating Gitea repositories.
    /// </summary>
    [ApiController]
    [Route("api/repositories")]
    [Authorize]
    [Produces("application/json")]
    public class RepositoryController : ControllerBase
    {
        private readonly IRepositoryService _repositoryService;
        private readonly IUserService _userService;
        private readonly ILogger<RepositoryController> _logger;

        public RepositoryController(IRepositoryService repositoryService, IUserService userService, ILogger<RepositoryController> logger)
        {
            _repositoryService = repositoryService ?? throw new ArgumentNullException(nameof(repositoryService));
            _userService       = userService ?? throw new ArgumentNullException(nameof(userService));
            _logger            = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private string CurrentUserId =>
            User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        private string CurrentEmail =>
            User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? string.Empty;

        private string ClientIp =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

        /// <summary>Returns a paginated list of repositories. All authenticated roles.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<RepositoryDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRepositories(
            [FromQuery] string? filter = null,
            [FromQuery] string? projectId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            var allowedProjectIds = await GetProjectScopeAsync(cancellationToken);
            var items = await _repositoryService.GetRepositoriesAsync(filter ?? string.Empty, page, pageSize, projectId, allowedProjectIds, cancellationToken);
            var pagination = PaginationInfo.Create(page, pageSize, items.Count);
            return Ok(ApiResponse<List<RepositoryDto>>.Ok(items, "Repositories retrieved successfully.", pagination));
        }

        private async Task<List<string>?> GetProjectScopeAsync(CancellationToken cancellationToken = default)
        {
            var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? string.Empty;

            if (role is "SuperAdmin" or "PlatformAdmin") return null;

            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return new List<string>();

            var user = await _userService.GetUserByIdAsync(userId, cancellationToken);
            var scope = user?.ProjectScope ?? new List<string>();

            if (scope.Count == 0 && role is not "QA")
                return null;

            return scope;
        }

        /// <summary>Returns a single repository by its identifier.</summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<RepositoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRepositoryById(string id, CancellationToken cancellationToken = default)
        {
            var repo = await _repositoryService.GetRepositoryByIdAsync(id, cancellationToken);
            if (repo == null)
                return NotFound(ApiResponse<RepositoryDto>.Fail($"Repository '{id}' was not found."));

            return Ok(ApiResponse<RepositoryDto>.Ok(repo));
        }

        /// <summary>Returns repositories associated with a project.</summary>
        [HttpGet("by-project/{projectId}")]
        [ProducesResponseType(typeof(ApiResponse<List<RepositoryDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByProjectId(string projectId, CancellationToken cancellationToken = default)
        {
            var repos = await _repositoryService.GetByProjectIdAsync(projectId, cancellationToken);
            return Ok(ApiResponse<List<RepositoryDto>>.Ok(repos, $"Repositories for project '{projectId}' retrieved."));
        }

        /// <summary>Registers a new Gitea repository. SuperAdmin and PlatformAdmin only.</summary>
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,PlatformAdmin")]
        [ProducesResponseType(typeof(ApiResponse<RepositoryDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterRepository(
            [FromBody] RegisterRepositoryRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<RepositoryDto>.Fail("Validation failed.", errors));
            }

            var created = await _repositoryService.RegisterRepositoryAsync(request, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
            return StatusCode(StatusCodes.Status201Created,
                ApiResponse<RepositoryDto>.Ok(created, "Repository registered successfully."));
        }

        /// <summary>Updates an existing repository's metadata. SuperAdmin and PlatformAdmin only.</summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "CanManageProjects")]
        [ProducesResponseType(typeof(ApiResponse<RepositoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateRepository(
            string id,
            [FromBody] UpdateRepositoryRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<RepositoryDto>.Fail("Validation failed.", errors));
            }

            var updated = await _repositoryService.UpdateRepositoryAsync(id, request, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
            return Ok(ApiResponse<RepositoryDto>.Ok(updated, "Repository updated successfully."));
        }

        /// <summary>Triggers a metadata sync from Gitea. SuperAdmin, PlatformAdmin, and ReleaseManager only.</summary>
        [HttpPost("{id}/sync")]
        [Authorize(Policy = "CanSyncRepository")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SyncRepository(string id, CancellationToken cancellationToken = default)
        {
            await _repositoryService.SyncFromGiteaAsync(id, cancellationToken);
            return Ok(ApiResponse.OkNoData("Repository synchronised successfully."));
        }

        /// <summary>Deactivates a repository. SuperAdmin and PlatformAdmin only.</summary>
        [HttpPost("{id}/deactivate")]
        [Authorize(Policy = "CanManageProjects")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeactivateRepository(string id, CancellationToken cancellationToken = default)
        {
            await _repositoryService.DeactivateRepositoryAsync(id, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
            return Ok(ApiResponse.OkNoData("Repository deactivated successfully."));
        }

        /// <summary>Permanently deletes a repository registration. SuperAdmin only.</summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteRepository(string id, CancellationToken cancellationToken = default)
        {
            await _repositoryService.DeleteRepositoryAsync(id, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
            return Ok(ApiResponse.OkNoData("Repository deleted successfully."));
        }
    }
}
