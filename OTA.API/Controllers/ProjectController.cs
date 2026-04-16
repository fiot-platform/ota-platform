using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OTA.API.Helpers;
using OTA.API.Models.DTOs;
using OTA.API.Services.Interfaces;
using System.Collections.Generic;

namespace OTA.API.Controllers
{
    /// <summary>
    /// Project management endpoints for creating, updating, and activating/deactivating projects.
    /// CustomerAdmin access is automatically scoped to their customer by AuthorizationScopeMiddleware.
    /// </summary>
    [ApiController]
    [Route("api/projects")]
    [Authorize]
    [Produces("application/json")]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;
        private readonly IUserService _userService;
        private readonly ILogger<ProjectController> _logger;

        public ProjectController(IProjectService projectService, IUserService userService, ILogger<ProjectController> logger)
        {
            _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
            _userService    = userService ?? throw new ArgumentNullException(nameof(userService));
            _logger         = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private string CurrentUserId =>
            User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        private string CurrentEmail =>
            User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? string.Empty;

        private string ClientIp =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

        /// <summary>Returns a paginated list of projects. All authenticated roles with view permission.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<ProjectDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProjects(
            [FromQuery] string? filter = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            var allowedProjectIds = await GetProjectScopeAsync(cancellationToken);
            var result = await _projectService.GetProjectsAsync(filter ?? string.Empty, page, pageSize, allowedProjectIds, cancellationToken);
            var pagination = PaginationInfo.Create(page, pageSize, result.TotalCount);
            return Ok(ApiResponse<List<ProjectDto>>.Ok(result.Items, "Projects retrieved successfully.", pagination));
        }

        private async Task<List<string>?> GetProjectScopeAsync(CancellationToken cancellationToken = default)
        {
            var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? string.Empty;
            if (role is "SuperAdmin" or "PlatformAdmin") return null;

            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return new List<string>();

            var user = await _userService.GetUserByIdAsync(userId, cancellationToken);
            return user?.ProjectScope ?? new List<string>();
        }

        /// <summary>Returns a single project by its identifier.</summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProjectById(string id, CancellationToken cancellationToken = default)
        {
            var project = await _projectService.GetProjectByIdAsync(id, cancellationToken);
            if (project == null)
                return NotFound(ApiResponse<ProjectDto>.Fail($"Project '{id}' was not found."));

            return Ok(ApiResponse<ProjectDto>.Ok(project));
        }

        /// <summary>Creates a new project. SuperAdmin and PlatformAdmin only.</summary>
        [HttpPost]
        [Authorize(Policy = "CanManageProjects")]
        [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateProject(
            [FromBody] CreateProjectRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<ProjectDto>.Fail("Validation failed.", errors));
            }

            var created = await _projectService.CreateProjectAsync(request, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
            return StatusCode(StatusCodes.Status201Created,
                ApiResponse<ProjectDto>.Ok(created, "Project created successfully."));
        }

        /// <summary>Updates an existing project's metadata. SuperAdmin and PlatformAdmin only.</summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "CanManageProjects")]
        [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateProject(
            string id,
            [FromBody] UpdateProjectRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<ProjectDto>.Fail("Validation failed.", errors));
            }

            var updated = await _projectService.UpdateProjectAsync(id, request, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
            return Ok(ApiResponse<ProjectDto>.Ok(updated, "Project updated successfully."));
        }

        /// <summary>Activates a project. SuperAdmin and PlatformAdmin only.</summary>
        [HttpPost("{id}/activate")]
        [Authorize(Policy = "CanManageProjects")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ActivateProject(string id, CancellationToken cancellationToken = default)
        {
            await _projectService.ActivateProjectAsync(id, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
            return Ok(ApiResponse.OkNoData("Project activated successfully."));
        }

        /// <summary>Permanently deletes a project. SuperAdmin only.</summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteProject(string id, CancellationToken cancellationToken = default)
        {
            await _projectService.DeleteProjectAsync(id, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
            return Ok(ApiResponse.OkNoData("Project deleted successfully."));
        }

        /// <summary>Deactivates a project. SuperAdmin and PlatformAdmin only.</summary>
        [HttpPost("{id}/deactivate")]
        [Authorize(Policy = "CanManageProjects")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeactivateProject(string id, CancellationToken cancellationToken = default)
        {
            await _projectService.DeactivateProjectAsync(id, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
            return Ok(ApiResponse.OkNoData("Project deactivated successfully."));
        }
    }
}
