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
    /// Implements Gitea repository registration, metadata synchronisation, and lifecycle management.
    /// </summary>
    public class RepositoryService : IRepositoryService
    {
        private readonly IRepositoryMasterRepository _repoRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IClientRepository _clientRepository;
        private readonly IGiteaApiService _giteaApiService;
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly ILogger<RepositoryService> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Initialises a new instance of <see cref="RepositoryService"/>.
        /// </summary>
        public RepositoryService(
            IRepositoryMasterRepository repoRepository,
            IProjectRepository projectRepository,
            IClientRepository clientRepository,
            IGiteaApiService giteaApiService,
            IAuditService auditService,
            INotificationService notificationService,
            IEmailService emailService,
            ILogger<RepositoryService> logger)
        {
            _repoRepository      = repoRepository      ?? throw new ArgumentNullException(nameof(repoRepository));
            _projectRepository   = projectRepository   ?? throw new ArgumentNullException(nameof(projectRepository));
            _clientRepository    = clientRepository    ?? throw new ArgumentNullException(nameof(clientRepository));
            _giteaApiService     = giteaApiService     ?? throw new ArgumentNullException(nameof(giteaApiService));
            _auditService        = auditService        ?? throw new ArgumentNullException(nameof(auditService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _emailService        = emailService        ?? throw new ArgumentNullException(nameof(emailService));
            _logger              = logger              ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<RepositoryDto> RegisterRepositoryAsync(
            RegisterRepositoryRequest request,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.GiteaOwner)) throw new ArgumentException("GiteaOwner is required.");
            if (string.IsNullOrWhiteSpace(request.GiteaRepoName)) throw new ArgumentException("GiteaRepoName is required.");
            if (string.IsNullOrWhiteSpace(request.ProjectId)) throw new ArgumentException("ProjectId is required.");

            // ── Step 1: Check if the repo already exists on Gitea ────────────────
            GiteaRepositoryDto? giteaRepo = null;
            try
            {
                giteaRepo = await _giteaApiService.GetRepositoryAsync(request.GiteaOwner, request.GiteaRepoName, cancellationToken);
                _logger.LogInformation("Gitea repo '{Owner}/{Repo}' already exists — linking.", request.GiteaOwner, request.GiteaRepoName);
            }
            catch (Exception)
            {
                // Repo not found or Gitea unreachable — attempt creation below
            }

            // ── Step 2: Create on Gitea if it doesn't exist ──────────────────────
            if (giteaRepo == null)
            {
                try
                {
                    giteaRepo = await _giteaApiService.CreateRepositoryForUserAsync(
                        request.GiteaOwner,
                        request.GiteaRepoName,
                        request.Description,
                        request.DefaultBranch,
                        request.IsPrivate,
                        cancellationToken);

                    _logger.LogInformation(
                        "Created Gitea repository '{Owner}/{Repo}' (id: {Id}).",
                        request.GiteaOwner, request.GiteaRepoName, giteaRepo.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Could not create Gitea repository '{Owner}/{Repo}'. Saving to OTA Platform with local metadata only.",
                        request.GiteaOwner, request.GiteaRepoName);
                }
            }

            // Use a synthetic id when Gitea is completely unavailable
            var giteaRepoId = giteaRepo?.Id.ToString()
                ?? $"local-{request.GiteaOwner}-{request.GiteaRepoName}".ToLowerInvariant();

            // Ensure no duplicate registration when Gitea id is real
            if (giteaRepo != null)
            {
                var existingByGiteaId = await _repoRepository.GetByGiteaRepoIdAsync(giteaRepoId, cancellationToken);
                if (existingByGiteaId != null)
                    throw new InvalidOperationException($"Repository '{request.GiteaOwner}/{request.GiteaRepoName}' is already registered (id: {existingByGiteaId.Id}).");
            }

            var (clientCode, clientName) = await ResolveClientAsync(request.ClientCode, cancellationToken);

            var entity = new RepositoryMasterEntity
            {
                GiteaRepoName = request.GiteaRepoName,
                Description = request.Description ?? giteaRepo?.Description,
                GiteaOwner = request.GiteaOwner,
                GiteaRepoId = giteaRepoId,
                GiteaUrl = giteaRepo?.HtmlUrl,
                ProjectId = request.ProjectId,
                ClientCode = clientCode,
                ClientName = clientName,
                DefaultBranch = request.DefaultBranch.Trim() is { Length: > 0 } b ? b : (giteaRepo?.DefaultBranch ?? "main"),
                IsActive = true,
                LastSyncedAt = giteaRepo != null ? DateTime.UtcNow : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedByUserId = callerUserId
            };

            await _repoRepository.InsertAsync(entity, cancellationToken);

            _logger.LogInformation("Repository '{Owner}/{Repo}' registered by '{Email}'.",
                request.GiteaOwner, request.GiteaRepoName, callerEmail);

            await _auditService.LogActionAsync(
                AuditAction.RepositoryRegistered,
                callerUserId, callerEmail, UserRole.SuperAdmin,
                "Repository", entity.Id,
                null,
                JsonSerializer.Serialize(new { entity.Id, entity.GiteaOwner, entity.GiteaRepoName, entity.ProjectId }, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);

            _ = _notificationService.NotifyAsync(
                "Repository Registered",
                $"Repository '{entity.GiteaOwner}/{entity.GiteaRepoName}' was registered.",
                new Dictionary<string, string> { ["type"] = "repository_registered", ["repositoryId"] = entity.Id, ["name"] = entity.GiteaRepoName },
                cancellationToken: CancellationToken.None);

            _ = _emailService.SendCrudNotificationAsync(callerEmail, callerEmail, "Created", "Repository", $"{entity.GiteaOwner}/{entity.GiteaRepoName}", CancellationToken.None);

            return MapToDto(entity);
        }

        /// <inheritdoc/>
        public async Task<RepositoryDto> UpdateRepositoryAsync(
            string repositoryId,
            UpdateRepositoryRequest request,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(repositoryId)) throw new ArgumentException("RepositoryId is required.", nameof(repositoryId));
            if (request == null) throw new ArgumentNullException(nameof(request));

            var entity = await _repoRepository.GetByIdAsync(repositoryId, cancellationToken)
                ?? throw new KeyNotFoundException($"Repository '{repositoryId}' not found.");

            var oldSnapshot = JsonSerializer.Serialize(
                new { entity.Name, entity.Description, entity.DefaultBranch, entity.IsActive, entity.ProjectId },
                _jsonOptions);

            if (!string.IsNullOrWhiteSpace(request.Name))
                entity.Name = request.Name.Trim();

            if (request.Description != null)
                entity.Description = request.Description.Trim();

            if (!string.IsNullOrWhiteSpace(request.DefaultBranch))
                entity.DefaultBranch = request.DefaultBranch.Trim();

            if (request.IsActive.HasValue)
                entity.IsActive = request.IsActive.Value;

            if (!string.IsNullOrWhiteSpace(request.ProjectId))
                entity.ProjectId = request.ProjectId.Trim();

            if (request.ClientCode != null)
            {
                var (clientCode, clientName) = await ResolveClientAsync(request.ClientCode, cancellationToken);
                entity.ClientCode = clientCode;
                entity.ClientName = clientName;
            }

            entity.UpdatedAt = DateTime.UtcNow;
            await _repoRepository.UpdateAsync(repositoryId, entity, cancellationToken);

            _logger.LogInformation("Repository '{RepositoryId}' updated by '{Email}'.", repositoryId, callerEmail);

            await _auditService.LogActionAsync(
                AuditAction.RepositoryUpdated,
                callerUserId, callerEmail, UserRole.SuperAdmin,
                "Repository", repositoryId,
                oldSnapshot,
                JsonSerializer.Serialize(
                    new { entity.Name, entity.Description, entity.DefaultBranch, entity.IsActive, entity.ProjectId },
                    _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);

            _ = _notificationService.NotifyAsync(
                "Repository Updated",
                $"Repository '{entity.GiteaRepoName}' was updated.",
                new Dictionary<string, string> { ["type"] = "repository_updated", ["repositoryId"] = repositoryId, ["name"] = entity.GiteaRepoName },
                cancellationToken: CancellationToken.None);

            return MapToDto(entity);
        }

        /// <inheritdoc/>
        public async Task SyncFromGiteaAsync(string repositoryId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(repositoryId)) throw new ArgumentException("RepositoryId is required.", nameof(repositoryId));

            var entity = await _repoRepository.GetByIdAsync(repositoryId, cancellationToken)
                ?? throw new KeyNotFoundException($"Repository '{repositoryId}' not found.");

            _logger.LogInformation("Syncing repository '{RepositoryId}' from Gitea.", repositoryId);

            await _giteaApiService.SyncRepositoryMetadataAsync(repositoryId, cancellationToken);

            entity.LastSyncedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            await _repoRepository.UpdateAsync(repositoryId, entity, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<RepositoryDto?> GetRepositoryByIdAsync(string repositoryId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(repositoryId)) throw new ArgumentException("RepositoryId is required.", nameof(repositoryId));

            var entity = await _repoRepository.GetByIdAsync(repositoryId, cancellationToken);
            if (entity == null) return null;

            string? projectName = null;
            string? clientName = null;
            if (!string.IsNullOrWhiteSpace(entity.ProjectId))
            {
                var project = MongoDB.Bson.ObjectId.TryParse(entity.ProjectId, out _)
                    ? await _projectRepository.GetByIdAsync(entity.ProjectId, cancellationToken)
                    : await _projectRepository.GetByProjectIdAsync(entity.ProjectId, cancellationToken);
                projectName = project?.Name;
                clientName = string.IsNullOrWhiteSpace(project?.CustomerName) ? null : project!.CustomerName;
            }

            return MapToDto(entity, projectName, clientName);
        }

        /// <inheritdoc/>
        public async Task<List<RepositoryDto>> GetRepositoriesAsync(string filter, int page, int pageSize, string? projectId = null, List<string>? allowedProjectIds = null, CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            // Always go through SearchAsync so allowedProjectIds scope is consistently applied.
            // When a specific projectId is requested, treat it as an additional filter.
            var effectiveAllowedIds = allowedProjectIds;
            if (!string.IsNullOrWhiteSpace(projectId))
            {
                // Intersect: if scope is null (admin), only the requested project; otherwise the intersection.
                effectiveAllowedIds = allowedProjectIds == null
                    ? new List<string> { projectId }
                    : allowedProjectIds.Contains(projectId) ? new List<string> { projectId } : new List<string>();
            }

            var items = await _repoRepository.SearchAsync(filter, page, pageSize, effectiveAllowedIds, cancellationToken);

            // Batch-load project names + client names to avoid N+1 queries
            var projectIds = items.Select(r => r.ProjectId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
            var projectNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var clientNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pid in projectIds)
            {
                var project = MongoDB.Bson.ObjectId.TryParse(pid, out _)
                    ? await _projectRepository.GetByIdAsync(pid, cancellationToken)
                    : await _projectRepository.GetByProjectIdAsync(pid, cancellationToken);
                if (project != null)
                {
                    projectNames[pid] = project.Name;
                    if (!string.IsNullOrWhiteSpace(project.CustomerName))
                        clientNames[pid] = project.CustomerName;
                }
            }

            return items.Select(r => MapToDto(
                r,
                projectNames.GetValueOrDefault(r.ProjectId ?? string.Empty),
                clientNames.GetValueOrDefault(r.ProjectId ?? string.Empty))).ToList();
        }

        /// <inheritdoc/>
        public async Task<List<RepositoryDto>> GetByProjectIdAsync(string projectId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(projectId)) throw new ArgumentException("ProjectId is required.", nameof(projectId));

            var items = await _repoRepository.GetByProjectIdAsync(projectId, cancellationToken);
            return items.Select(r => MapToDto(r)).ToList();
        }

        /// <inheritdoc/>
        public async Task DeleteRepositoryAsync(
            string repositoryId,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(repositoryId)) throw new ArgumentException("RepositoryId is required.", nameof(repositoryId));

            var entity = await _repoRepository.GetByIdAsync(repositoryId, cancellationToken)
                ?? throw new KeyNotFoundException($"Repository '{repositoryId}' not found.");

            await _repoRepository.DeleteAsync(repositoryId, cancellationToken);

            _logger.LogInformation("Repository '{RepositoryId}' permanently deleted by '{Email}'.", repositoryId, callerEmail);

            await _auditService.LogActionAsync(
                AuditAction.RepositoryDeleted,
                callerUserId, callerEmail, UserRole.SuperAdmin,
                "Repository", repositoryId,
                JsonSerializer.Serialize(new { entity.GiteaOwner, entity.GiteaRepoName, entity.ProjectId }, _jsonOptions),
                null,
                ipAddress,
                cancellationToken: cancellationToken);

            _ = _notificationService.NotifyAsync(
                "Repository Deleted",
                $"Repository '{entity.GiteaOwner}/{entity.GiteaRepoName}' was permanently deleted.",
                new Dictionary<string, string> { ["type"] = "repository_deleted", ["repositoryId"] = repositoryId, ["name"] = entity.GiteaRepoName },
                cancellationToken: CancellationToken.None);
        }

        /// <inheritdoc/>
        public async Task DeactivateRepositoryAsync(
            string repositoryId,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(repositoryId)) throw new ArgumentException("RepositoryId is required.", nameof(repositoryId));

            var entity = await _repoRepository.GetByIdAsync(repositoryId, cancellationToken)
                ?? throw new KeyNotFoundException($"Repository '{repositoryId}' not found.");

            if (!entity.IsActive) throw new InvalidOperationException("Repository is already inactive.");

            entity.IsActive = false;
            entity.UpdatedAt = DateTime.UtcNow;
            await _repoRepository.UpdateAsync(repositoryId, entity, cancellationToken);

            _logger.LogInformation("Repository '{RepositoryId}' deactivated by '{Email}'.", repositoryId, callerEmail);

            await _auditService.LogActionAsync(
                AuditAction.RepositoryDeactivated,
                callerUserId, callerEmail, UserRole.SuperAdmin,
                "Repository", repositoryId,
                JsonSerializer.Serialize(new { IsActive = true }, _jsonOptions),
                JsonSerializer.Serialize(new { IsActive = false }, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);

            _ = _notificationService.NotifyAsync(
                "Repository Deactivated",
                $"Repository '{entity.GiteaOwner}/{entity.GiteaRepoName}' was deactivated.",
                new Dictionary<string, string> { ["type"] = "repository_deactivated", ["repositoryId"] = repositoryId, ["name"] = entity.GiteaRepoName },
                cancellationToken: CancellationToken.None);
        }

        // ── Private helpers ─────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves a client code to a (code, name) tuple. An empty/null/whitespace input
        /// returns (null, null) so the entity's client fields are cleared.
        /// </summary>
        private async Task<(string? code, string? name)> ResolveClientAsync(string? clientCode, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(clientCode)) return (null, null);
            var trimmed = clientCode.Trim();
            var client = await _clientRepository.GetByCodeAsync(trimmed, cancellationToken);
            if (client == null)
                throw new InvalidOperationException($"Client with code '{trimmed}' was not found.");
            return (client.Code, client.Name);
        }

        // ── Private mapper ──────────────────────────────────────────────────────────

        private static RepositoryDto MapToDto(RepositoryMasterEntity e, string? projectName = null, string? projectClientName = null) => new RepositoryDto
        {
            Id = e.Id,
            RepositoryId = e.Id,
            Name = e.GiteaRepoName,
            GiteaRepo = e.GiteaRepoName,
            GiteaRepoName = e.GiteaRepoName,
            Description = e.Description,
            GiteaOwner = e.GiteaOwner,
            GiteaRepoId = long.TryParse(e.GiteaRepoId, out var giteaId) ? giteaId : 0,
            GiteaUrl = e.GiteaUrl,
            ProjectId = e.ProjectId,
            ProjectName = projectName,
            // Prefer the per-repo client tag when present; fall back to the project's primary client.
            ClientCode = e.ClientCode,
            ClientName = !string.IsNullOrWhiteSpace(e.ClientName) ? e.ClientName : projectClientName,
            DefaultBranch = e.DefaultBranch,
            IsActive = e.IsActive,
            WebhookConfigured = e.WebhookConfigured,
            LastSyncedAt = e.LastSyncedAt,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }
}
