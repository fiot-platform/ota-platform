using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OTA.API.Models.DTOs;
using OTA.API.Models.Entities;
using OTA.API.Models.Enums;
using OTA.API.Repositories.Interfaces;
using OTA.API.Services.Interfaces;

namespace OTA.API.Services
{
    /// <summary>
    /// Implements project lifecycle management operations with full audit logging on every state change.
    /// </summary>
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IAuditService _auditService;
        private readonly ILogger<ProjectService> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Initialises a new instance of <see cref="ProjectService"/>.
        /// </summary>
        public ProjectService(
            IProjectRepository projectRepository,
            IAuditService auditService,
            ILogger<ProjectService> logger)
        {
            _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<ProjectDto> CreateProjectAsync(
            CreateProjectRequest request,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Project name is required.");

            var project = new ProjectEntity
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                CustomerId = request.CustomerId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedByUserId = callerUserId
            };

            await _projectRepository.InsertAsync(project, cancellationToken);

            _logger.LogInformation("Project '{Name}' created by '{Email}'.", project.Name, callerEmail);

            await _auditService.LogActionAsync(
                AuditAction.ProjectCreated,
                callerUserId, callerEmail, UserRole.SuperAdmin,
                "Project", project.Id,
                null,
                JsonSerializer.Serialize(new { project.Id, project.Name, project.CustomerId }, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);

            return MapToDto(project);
        }

        /// <inheritdoc/>
        public async Task<ProjectDto> UpdateProjectAsync(
            string projectId,
            UpdateProjectRequest request,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(projectId)) throw new ArgumentException("ProjectId is required.", nameof(projectId));
            if (request == null) throw new ArgumentNullException(nameof(request));

            var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken)
                ?? throw new KeyNotFoundException($"Project '{projectId}' not found.");

            var oldSnapshot = JsonSerializer.Serialize(new { project.Name, project.Description }, _jsonOptions);

            if (!string.IsNullOrWhiteSpace(request.Name))
                project.Name = request.Name.Trim();

            if (request.Description != null)
                project.Description = request.Description.Trim();

            project.UpdatedAt = DateTime.UtcNow;
            await _projectRepository.UpdateAsync(projectId, project, cancellationToken);

            _logger.LogInformation("Project '{ProjectId}' updated by '{Email}'.", projectId, callerEmail);

            await _auditService.LogActionAsync(
                AuditAction.ProjectUpdated,
                callerUserId, callerEmail, UserRole.SuperAdmin,
                "Project", projectId,
                oldSnapshot,
                JsonSerializer.Serialize(new { project.Name, project.Description }, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);

            return MapToDto(project);
        }

        /// <inheritdoc/>
        public async Task ActivateProjectAsync(
            string projectId,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(projectId)) throw new ArgumentException("ProjectId is required.", nameof(projectId));

            var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken)
                ?? throw new KeyNotFoundException($"Project '{projectId}' not found.");

            if (project.IsActive) throw new InvalidOperationException("Project is already active.");

            project.IsActive = true;
            project.UpdatedAt = DateTime.UtcNow;
            await _projectRepository.UpdateAsync(projectId, project, cancellationToken);

            _logger.LogInformation("Project '{ProjectId}' activated by '{Email}'.", projectId, callerEmail);

            await _auditService.LogActionAsync(
                AuditAction.ProjectActivated,
                callerUserId, callerEmail, UserRole.SuperAdmin,
                "Project", projectId,
                JsonSerializer.Serialize(new { IsActive = false }, _jsonOptions),
                JsonSerializer.Serialize(new { IsActive = true }, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        public async Task DeactivateProjectAsync(
            string projectId,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(projectId)) throw new ArgumentException("ProjectId is required.", nameof(projectId));

            var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken)
                ?? throw new KeyNotFoundException($"Project '{projectId}' not found.");

            if (!project.IsActive) throw new InvalidOperationException("Project is already inactive.");

            project.IsActive = false;
            project.UpdatedAt = DateTime.UtcNow;
            await _projectRepository.UpdateAsync(projectId, project, cancellationToken);

            _logger.LogInformation("Project '{ProjectId}' deactivated by '{Email}'.", projectId, callerEmail);

            await _auditService.LogActionAsync(
                AuditAction.ProjectDeactivated,
                callerUserId, callerEmail, UserRole.SuperAdmin,
                "Project", projectId,
                JsonSerializer.Serialize(new { IsActive = true }, _jsonOptions),
                JsonSerializer.Serialize(new { IsActive = false }, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<ProjectDto?> GetProjectByIdAsync(string projectId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(projectId)) throw new ArgumentException("ProjectId is required.", nameof(projectId));

            var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
            return project == null ? null : MapToDto(project);
        }

        /// <inheritdoc/>
        public async Task<PagedResult<ProjectDto>> GetProjectsAsync(string filter, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var items = await _projectRepository.SearchAsync(filter, page, pageSize, cancellationToken);
            var total = await _projectRepository.CountAsync(filter, cancellationToken);

            return new PagedResult<ProjectDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        /// <inheritdoc/>
        public async Task<List<ProjectDto>> GetProjectsByCustomerAsync(string customerId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(customerId)) throw new ArgumentException("CustomerId is required.", nameof(customerId));

            var projects = await _projectRepository.GetByCustomerIdAsync(customerId, cancellationToken);
            return projects.Select(MapToDto).ToList();
        }

        // ── Private mapper ──────────────────────────────────────────────────────────

        private static ProjectDto MapToDto(ProjectEntity p) => new ProjectDto
        {
            Id = p.Id,
            ProjectId = p.Id,
            Name = p.Name,
            Description = p.Description,
            CustomerId = p.CustomerId,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            CreatedByUserId = p.CreatedByUserId
        };
    }
}
