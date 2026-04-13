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
        private readonly IGiteaApiService _giteaApiService;
        private readonly IAuditService _auditService;
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
            IGiteaApiService giteaApiService,
            IAuditService auditService,
            ILogger<RepositoryService> logger)
        {
            _repoRepository = repoRepository ?? throw new ArgumentNullException(nameof(repoRepository));
            _giteaApiService = giteaApiService ?? throw new ArgumentNullException(nameof(giteaApiService));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

            var entity = new RepositoryMasterEntity
            {
                GiteaRepoName = request.GiteaRepoName,
                Description = request.Description ?? giteaRepo?.Description,
                GiteaOwner = request.GiteaOwner,
                GiteaRepoId = giteaRepoId,
                GiteaUrl = giteaRepo?.HtmlUrl,
                ProjectId = request.ProjectId,
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

            var oldSnapshot = JsonSerializer.Serialize(new { entity.Name, entity.Description }, _jsonOptions);

            if (!string.IsNullOrWhiteSpace(request.Name))
                entity.Name = request.Name.Trim();

            if (request.Description != null)
                entity.Description = request.Description.Trim();

            entity.UpdatedAt = DateTime.UtcNow;
            await _repoRepository.UpdateAsync(repositoryId, entity, cancellationToken);

            _logger.LogInformation("Repository '{RepositoryId}' updated by '{Email}'.", repositoryId, callerEmail);

            await _auditService.LogActionAsync(
                AuditAction.RepositoryUpdated,
                callerUserId, callerEmail, UserRole.SuperAdmin,
                "Repository", repositoryId,
                oldSnapshot,
                JsonSerializer.Serialize(new { entity.Name, entity.Description }, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);

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
            return entity == null ? null : MapToDto(entity);
        }

        /// <inheritdoc/>
        public async Task<List<RepositoryDto>> GetRepositoriesAsync(string filter, int page, int pageSize, string? projectId = null, CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            if (!string.IsNullOrWhiteSpace(projectId))
            {
                var projectItems = await _repoRepository.GetByProjectIdAsync(projectId, cancellationToken);
                return projectItems.Select(MapToDto).ToList();
            }

            var items = await _repoRepository.SearchAsync(filter, page, pageSize, cancellationToken);
            return items.Select(MapToDto).ToList();
        }

        /// <inheritdoc/>
        public async Task<List<RepositoryDto>> GetByProjectIdAsync(string projectId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(projectId)) throw new ArgumentException("ProjectId is required.", nameof(projectId));

            var items = await _repoRepository.GetByProjectIdAsync(projectId, cancellationToken);
            return items.Select(MapToDto).ToList();
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
        }

        // ── Private mapper ──────────────────────────────────────────────────────────

        private static RepositoryDto MapToDto(RepositoryMasterEntity e) => new RepositoryDto
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
            DefaultBranch = e.DefaultBranch,
            IsActive = e.IsActive,
            WebhookConfigured = e.WebhookConfigured,
            LastSyncedAt = e.LastSyncedAt,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };
    }
}
