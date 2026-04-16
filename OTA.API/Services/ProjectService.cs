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
        private readonly INotificationService _notificationService;
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
            INotificationService notificationService,
            ILogger<ProjectService> logger)
        {
            _projectRepository   = projectRepository   ?? throw new ArgumentNullException(nameof(projectRepository));
            _auditService        = auditService        ?? throw new ArgumentNullException(nameof(auditService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger              = logger              ?? throw new ArgumentNullException(nameof(logger));
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
                CustomerName = request.CustomerName?.Trim() ?? string.Empty,
                BusinessUnit = request.BusinessUnit?.Trim(),
                GiteaOrgName = request.GiteaOrgName?.Trim(),
                Tags = request.Tags ?? new List<string>(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedByUserId = callerUserId
            };

            await _projectRepository.InsertAsync(project, cancellationToken);

            // Backfill ProjectId with the MongoDB-assigned ObjectId so lookups by projectId field work
            if (string.IsNullOrWhiteSpace(project.ProjectId))
            {
                project.ProjectId = project.Id;
                await _projectRepository.UpdateAsync(project.Id, project, cancellationToken);
            }

            _logger.LogInformation("Project '{Name}' created by '{Email}'.", project.Name, callerEmail);

            await _auditService.LogActionAsync(
                AuditAction.ProjectCreated,
                callerUserId, callerEmail, UserRole.SuperAdmin,
                "Project", project.Id,
                null,
                JsonSerializer.Serialize(new { project.Id, project.Name, project.CustomerId }, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);

            _ = _notificationService.NotifyAsync(
                "Project Created",
                $"New project '{project.Name}' was created.",
                new Dictionary<string, string> { ["type"] = "project_created", ["projectId"] = project.Id, ["name"] = project.Name },
                cancellationToken: CancellationToken.None);

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

            if (request.BusinessUnit != null)
                project.BusinessUnit = request.BusinessUnit.Trim();

            if (request.GiteaOrgName != null)
                project.GiteaOrgName = request.GiteaOrgName.Trim();

            if (request.Tags != null)
                project.Tags = request.Tags;

            if (request.IsActive.HasValue)
                project.IsActive = request.IsActive.Value;

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

            _ = _notificationService.NotifyAsync(
                "Project Updated",
                $"Project '{project.Name}' was updated.",
                new Dictionary<string, string> { ["type"] = "project_updated", ["projectId"] = projectId, ["name"] = project.Name },
                cancellationToken: CancellationToken.None);

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

            _ = _notificationService.NotifyAsync(
                "Project Activated",
                $"Project '{project.Name}' was activated.",
                new Dictionary<string, string> { ["type"] = "project_activated", ["projectId"] = projectId },
                cancellationToken: CancellationToken.None);
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

            _ = _notificationService.NotifyAsync(
                "Project Deactivated",
                $"Project '{project.Name}' was deactivated.",
                new Dictionary<string, string> { ["type"] = "project_deactivated", ["projectId"] = projectId },
                cancellationToken: CancellationToken.None);
        }

        /// <inheritdoc/>
        public async Task DeleteProjectAsync(
            string projectId,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(projectId)) throw new ArgumentException("ProjectId is required.", nameof(projectId));

            var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken)
                ?? throw new KeyNotFoundException($"Project '{projectId}' not found.");

            await _projectRepository.DeleteAsync(projectId, cancellationToken);

            _logger.LogInformation("Project '{ProjectId}' permanently deleted by '{Email}'.", projectId, callerEmail);

            await _auditService.LogActionAsync(
                AuditAction.ProjectDeleted,
                callerUserId, callerEmail, UserRole.SuperAdmin,
                "Project", projectId,
                JsonSerializer.Serialize(new { project.Name, project.CustomerId }, _jsonOptions),
                null,
                ipAddress,
                cancellationToken: cancellationToken);

            _ = _notificationService.NotifyAsync(
                "Project Deleted",
                $"Project '{project.Name}' was permanently deleted.",
                new Dictionary<string, string> { ["type"] = "project_deleted", ["projectId"] = projectId, ["name"] = project.Name },
                cancellationToken: CancellationToken.None);
        }

        /// <inheritdoc/>
        public async Task<ProjectDto?> GetProjectByIdAsync(string projectId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(projectId)) throw new ArgumentException("ProjectId is required.", nameof(projectId));

            var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
            return project == null ? null : MapToDto(project);
        }

        /// <inheritdoc/>
        public async Task<PagedResult<ProjectDto>> GetProjectsAsync(string filter, int page, int pageSize, List<string>? allowedProjectIds = null, CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var items = await _projectRepository.SearchAsync(filter, page, pageSize, allowedProjectIds, cancellationToken);
            var total = await _projectRepository.CountAsync(filter, allowedProjectIds, cancellationToken);

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
            CustomerName = p.CustomerName,
            BusinessUnit = p.BusinessUnit,
            GiteaOrgName = p.GiteaOrgName,
            Tags = p.Tags ?? new List<string>(),
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            CreatedByUserId = p.CreatedByUserId
        };
    }
}
