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
    /// Implements the firmware lifecycle including the multi-stage approval workflow:
    /// Draft -> PendingQA -> QAVerified -> PendingApproval -> Approved | Rejected.
    /// All status transitions are validated and audited.
    /// </summary>
    public class FirmwareService : IFirmwareService
    {
        private readonly IFirmwareRepository _firmwareRepository;
        private readonly IRepositoryMasterRepository _repoRepository;
        private readonly IGiteaApiService _giteaApiService;
        private readonly IAuditService _auditService;
        private readonly ILogger<FirmwareService> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Initialises a new instance of <see cref="FirmwareService"/>.
        /// </summary>
        public FirmwareService(
            IFirmwareRepository firmwareRepository,
            IRepositoryMasterRepository repoRepository,
            IGiteaApiService giteaApiService,
            IAuditService auditService,
            ILogger<FirmwareService> logger)
        {
            _firmwareRepository = firmwareRepository ?? throw new ArgumentNullException(nameof(firmwareRepository));
            _repoRepository = repoRepository ?? throw new ArgumentNullException(nameof(repoRepository));
            _giteaApiService = giteaApiService ?? throw new ArgumentNullException(nameof(giteaApiService));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<FirmwareDto> CreateFirmwareAsync(
            CreateFirmwareRequest request,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.Version)) throw new ArgumentException("Version is required.");
            if (string.IsNullOrWhiteSpace(request.RepositoryId)) throw new ArgumentException("RepositoryId is required.");

            var repo = await _repoRepository.GetByIdAsync(request.RepositoryId, cancellationToken)
                ?? throw new KeyNotFoundException($"Repository '{request.RepositoryId}' not found.");

            var entity = new FirmwareVersionEntity
            {
                FirmwareId = Guid.NewGuid().ToString(),
                Version = request.Version.Trim(),
                RepositoryId = request.RepositoryId,
                ProjectId = repo.ProjectId,
                GiteaReleaseId = request.GiteaReleaseId,
                GiteaTagName = request.GiteaTagName?.Trim() ?? string.Empty,
                DownloadUrl = request.DownloadUrl,
                FileSha256 = request.FileSha256,
                FileSizeBytes = request.FileSizeBytes,
                FileName = request.FileName ?? string.Empty,
                SupportedModels = request.SupportedModels ?? new List<string>(),
                SupportedHardwareRevisions = request.SupportedHardwareRevisions ?? new List<string>(),
                Channel = request.Channel,
                Status = FirmwareStatus.Draft,
                ReleaseNotes = request.ReleaseNotes,
                IsMandate = request.IsMandate,
                MinRequiredVersion = request.MinRequiredVersion,
                MaxAllowedVersion = request.MaxAllowedVersion,
                CreatedByUserId = callerUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _firmwareRepository.InsertAsync(entity, cancellationToken);

            _logger.LogInformation("Firmware '{Version}' created in repository '{RepoId}' by '{Email}'.",
                entity.Version, entity.RepositoryId, callerEmail);

            await _auditService.LogActionAsync(
                AuditAction.FirmwareCreated,
                callerUserId, callerEmail, UserRole.SuperAdmin,
                "Firmware", entity.Id,
                null,
                JsonSerializer.Serialize(new { entity.Id, entity.Version, entity.Status }, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);

            return MapToDto(entity, repo.GiteaRepoName);
        }

        /// <inheritdoc/>
        public async Task<FirmwareDto> UpdateFirmwareAsync(
            string firmwareId,
            UpdateFirmwareRequest request,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(firmwareId)) throw new ArgumentException("FirmwareId is required.", nameof(firmwareId));
            if (request == null) throw new ArgumentNullException(nameof(request));

            var entity = await _firmwareRepository.GetByFirmwareIdAsync(firmwareId, cancellationToken)
                ?? throw new KeyNotFoundException($"Firmware '{firmwareId}' not found.");

            if (entity.Status == FirmwareStatus.Approved || entity.Status == FirmwareStatus.Deprecated)
                throw new InvalidOperationException($"Cannot update firmware in '{entity.Status}' status.");

            var oldSnapshot = JsonSerializer.Serialize(new { entity.ReleaseNotes, entity.SupportedModels }, _jsonOptions);

            if (request.ReleaseNotes != null) entity.ReleaseNotes = request.ReleaseNotes;
            if (request.SupportedModels != null) entity.SupportedModels = request.SupportedModels;
            if (request.SupportedHardwareRevisions != null) entity.SupportedHardwareRevisions = request.SupportedHardwareRevisions;
            if (request.IsMandate.HasValue) entity.IsMandate = request.IsMandate.Value;
            if (request.MinRequiredVersion != null) entity.MinRequiredVersion = request.MinRequiredVersion;
            if (request.MaxAllowedVersion != null) entity.MaxAllowedVersion = request.MaxAllowedVersion;

            entity.UpdatedAt = DateTime.UtcNow;
            await _firmwareRepository.UpdateAsync(entity.Id, entity, cancellationToken);

            await _auditService.LogActionAsync(
                AuditAction.FirmwareUpdated,
                callerUserId, callerEmail, UserRole.SuperAdmin,
                "Firmware", firmwareId,
                oldSnapshot,
                JsonSerializer.Serialize(new { entity.ReleaseNotes }, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);

            return MapToDto(entity);
        }

        /// <inheritdoc/>
        public async Task<FirmwareDto> ApproveFirmwareAsync(
            string id,
            string userId,
            string notes,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            var entity = await _firmwareRepository.GetByFirmwareIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException($"Firmware '{id}' not found.");

            if (entity.Status != FirmwareStatus.PendingApproval && entity.Status != FirmwareStatus.QAVerified)
                throw new InvalidOperationException($"Firmware must be in PendingApproval or QAVerified status to approve. Current status: {entity.Status}.");

            var oldStatus = entity.Status;
            entity.Status = FirmwareStatus.Approved;
            entity.ApprovedByUserId = userId;
            entity.ApprovedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(notes)) entity.ApprovalNotes = notes;

            await _firmwareRepository.UpdateAsync(entity.Id, entity, cancellationToken);

            _logger.LogInformation("Firmware '{Id}' approved by user '{UserId}'.", id, userId);

            var approver = await GetUserEmailForAudit(userId);
            await _auditService.LogActionAsync(
                AuditAction.FirmwareApproved,
                userId, approver, UserRole.SuperAdmin,
                "Firmware", id,
                JsonSerializer.Serialize(new { Status = oldStatus.ToString() }, _jsonOptions),
                JsonSerializer.Serialize(new { Status = entity.Status.ToString(), Notes = notes }, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);

            return MapToDto(entity);
        }

        /// <inheritdoc/>
        public async Task<FirmwareDto> RejectFirmwareAsync(
            string id,
            string userId,
            string reason,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Rejection reason is mandatory.", nameof(reason));

            var entity = await _firmwareRepository.GetByFirmwareIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException($"Firmware '{id}' not found.");

            var rejectableStatuses = new[] { FirmwareStatus.PendingApproval, FirmwareStatus.QAVerified, FirmwareStatus.PendingQA, FirmwareStatus.Draft };
            if (!rejectableStatuses.Contains(entity.Status))
                throw new InvalidOperationException($"Cannot reject firmware in '{entity.Status}' status.");

            var oldStatus = entity.Status;
            entity.Status = FirmwareStatus.Rejected;
            entity.RejectionReason = reason;
            entity.RejectedByUserId = userId;
            entity.RejectedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            await _firmwareRepository.UpdateAsync(entity.Id, entity, cancellationToken);

            _logger.LogInformation("Firmware '{Id}' rejected by user '{UserId}'. Reason: {Reason}.", id, userId, reason);

            var rejector = await GetUserEmailForAudit(userId);
            await _auditService.LogActionAsync(
                AuditAction.FirmwareRejected,
                userId, rejector, UserRole.SuperAdmin,
                "Firmware", id,
                JsonSerializer.Serialize(new { Status = oldStatus.ToString() }, _jsonOptions),
                JsonSerializer.Serialize(new { Status = entity.Status.ToString(), Reason = reason }, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);

            return MapToDto(entity);
        }

        /// <inheritdoc/>
        public async Task<FirmwareDto> QAVerifyFirmwareAsync(
            string id,
            string userId,
            string remarks,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            var entity = await _firmwareRepository.GetByFirmwareIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException($"Firmware '{id}' not found.");

            if (entity.Status != FirmwareStatus.PendingQA && entity.Status != FirmwareStatus.Draft)
                throw new InvalidOperationException($"Firmware must be in Draft or PendingQA status for QA verification. Current status: {entity.Status}.");

            var oldStatus = entity.Status;
            entity.Status = FirmwareStatus.QAVerified;
            entity.QaVerifiedByUserId = userId;
            entity.QaVerifiedAt = DateTime.UtcNow;
            entity.QaRemarks = remarks;
            // Automatically move to PendingApproval after QA verification
            entity.Status = FirmwareStatus.PendingApproval;
            entity.UpdatedAt = DateTime.UtcNow;

            await _firmwareRepository.UpdateAsync(entity.Id, entity, cancellationToken);

            _logger.LogInformation("Firmware '{Id}' QA-verified by user '{UserId}', moved to PendingApproval.", id, userId);

            var qaUser = await GetUserEmailForAudit(userId);
            await _auditService.LogActionAsync(
                AuditAction.FirmwareQAVerified,
                userId, qaUser, UserRole.QA,
                "Firmware", id,
                JsonSerializer.Serialize(new { Status = oldStatus.ToString() }, _jsonOptions),
                JsonSerializer.Serialize(new { Status = entity.Status.ToString(), Remarks = remarks }, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);

            return MapToDto(entity);
        }

        /// <inheritdoc/>
        public async Task<FirmwareDto> AssignChannelAsync(
            string firmwareId,
            FirmwareChannel channel,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            var entity = await _firmwareRepository.GetByFirmwareIdAsync(firmwareId, cancellationToken)
                ?? throw new KeyNotFoundException($"Firmware '{firmwareId}' not found.");

            var oldChannel = entity.Channel;
            entity.Channel = channel;
            entity.UpdatedAt = DateTime.UtcNow;
            await _firmwareRepository.UpdateAsync(entity.Id, entity, cancellationToken);

            await _auditService.LogActionAsync(
                AuditAction.FirmwareChannelAssigned,
                callerUserId, callerEmail, UserRole.SuperAdmin,
                "Firmware", firmwareId,
                JsonSerializer.Serialize(new { Channel = oldChannel.ToString() }, _jsonOptions),
                JsonSerializer.Serialize(new { Channel = channel.ToString() }, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);

            return MapToDto(entity);
        }

        /// <inheritdoc/>
        public async Task<FirmwareDto?> GetFirmwareByIdAsync(string firmwareId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(firmwareId)) throw new ArgumentException("FirmwareId is required.", nameof(firmwareId));

            // Look up by platform FirmwareId (GUID); the base GetByIdAsync is for MongoDB _id only
            var entity = await _firmwareRepository.GetByFirmwareIdAsync(firmwareId, cancellationToken);
            if (entity == null) return null;

            string? repoName = null;
            if (!string.IsNullOrWhiteSpace(entity.RepositoryId))
            {
                var repo = await _repoRepository.GetByIdAsync(entity.RepositoryId, cancellationToken);
                repoName = repo?.GiteaRepoName;
            }

            return MapToDto(entity, repoName);
        }

        /// <inheritdoc/>
        public async Task<PagedResult<FirmwareDto>> GetFirmwareListAsync(
            string search,
            string? status,
            string? channel,
            string? projectId,
            string? repositoryId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var (items, totalCount) = await _firmwareRepository.SearchWithFiltersAsync(
                search, status, channel, projectId, repositoryId, page, pageSize, cancellationToken);

            // Batch-load repository names to avoid N+1 queries
            var repoIds = items.Select(f => f.RepositoryId).Distinct().ToList();
            var repoNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var rid in repoIds)
            {
                if (string.IsNullOrWhiteSpace(rid)) continue;
                var repo = await _repoRepository.GetByIdAsync(rid, cancellationToken);
                if (repo != null) repoNames[rid] = repo.GiteaRepoName;
            }

            return new PagedResult<FirmwareDto>
            {
                Items = items.Select(e => MapToDto(e, repoNames.GetValueOrDefault(e.RepositoryId))).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        /// <inheritdoc/>
        public async Task<int> SyncFirmwareFromGiteaAsync(string repositoryId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(repositoryId)) throw new ArgumentException("RepositoryId is required.", nameof(repositoryId));

            var repo = await _repoRepository.GetByIdAsync(repositoryId, cancellationToken)
                ?? throw new KeyNotFoundException($"Repository '{repositoryId}' not found.");

            _logger.LogInformation("Syncing firmware from Gitea for repository '{RepoId}'.", repositoryId);

            var releases = await _giteaApiService.GetReleasesAsync(repo.GiteaOwner, repo.GiteaRepoName, cancellationToken);
            var existingFirmware = await _firmwareRepository.GetByRepositoryIdAsync(repositoryId, cancellationToken);
            var existingTags = new HashSet<string>(existingFirmware.Select(f => f.GiteaTagName ?? string.Empty), StringComparer.OrdinalIgnoreCase);

            int newCount = 0;
            foreach (var release in releases)
            {
                if (string.IsNullOrWhiteSpace(release.TagName) || existingTags.Contains(release.TagName))
                    continue;

                var firmware = new FirmwareVersionEntity
                {
                    FirmwareId = Guid.NewGuid().ToString(),
                    Version = release.TagName.TrimStart('v'),
                    RepositoryId = repositoryId,
                    ProjectId = repo.ProjectId,
                    GiteaTagName = release.TagName,
                    DownloadUrl = release.Assets?.FirstOrDefault()?.BrowserDownloadUrl ?? string.Empty,
                    FileName = release.Assets?.FirstOrDefault()?.Name ?? string.Empty,
                    Status = FirmwareStatus.Draft,
                    Channel = FirmwareChannel.Alpha,
                    SupportedModels = new List<string>(),
                    SupportedHardwareRevisions = new List<string>(),
                    ReleaseNotes = release.Body,
                    CreatedAt = release.CreatedAt != default ? release.CreatedAt : DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _firmwareRepository.InsertAsync(firmware, cancellationToken);
                existingTags.Add(release.TagName);
                newCount++;
            }

            _logger.LogInformation("Gitea sync for repository '{RepoId}': {Count} new firmware records created.", repositoryId, newCount);
            return newCount;
        }

        /// <inheritdoc/>
        public async Task DeprecateFirmwareAsync(
            string firmwareId,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            var entity = await _firmwareRepository.GetByFirmwareIdAsync(firmwareId, cancellationToken)
                ?? throw new KeyNotFoundException($"Firmware '{firmwareId}' not found.");

            if (entity.Status != FirmwareStatus.Approved)
                throw new InvalidOperationException($"Only Approved firmware can be deprecated. Current status: {entity.Status}.");

            var oldStatus = entity.Status;
            entity.Status = FirmwareStatus.Deprecated;
            entity.UpdatedAt = DateTime.UtcNow;
            await _firmwareRepository.UpdateAsync(entity.Id, entity, cancellationToken);

            _logger.LogInformation("Firmware '{FirmwareId}' deprecated by '{Email}'.", firmwareId, callerEmail);

            await _auditService.LogActionAsync(
                AuditAction.FirmwareDeprecated,
                callerUserId, callerEmail, UserRole.SuperAdmin,
                "Firmware", firmwareId,
                JsonSerializer.Serialize(new { Status = oldStatus.ToString() }, _jsonOptions),
                JsonSerializer.Serialize(new { Status = FirmwareStatus.Deprecated.ToString() }, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);
        }

        // ── Private helpers ─────────────────────────────────────────────────────────

        private async Task<string> GetUserEmailForAudit(string userId)
        {
            try
            {
                // Best-effort; if lookup fails we fall back to the userId itself
                return userId;
            }
            catch
            {
                return userId;
            }
        }

        private static FirmwareDto MapToDto(FirmwareVersionEntity e, string? repositoryName = null) => new FirmwareDto
        {
            FirmwareId = e.FirmwareId,
            RepositoryId = e.RepositoryId,
            RepositoryName = repositoryName,
            ProjectId = e.ProjectId,
            Version = e.Version,
            GiteaReleaseId = e.GiteaReleaseId,
            GiteaTagName = e.GiteaTagName,
            ReleaseNotes = e.ReleaseNotes,
            FileName = e.FileName,
            FileSha256 = e.FileSha256,
            FileSizeBytes = e.FileSizeBytes,
            DownloadUrl = e.DownloadUrl,
            Channel = e.Channel.ToString(),
            Status = e.Status.ToString(),
            IsMandate = e.IsMandate,
            SupportedModels = e.SupportedModels,
            SupportedHardwareRevisions = e.SupportedHardwareRevisions,
            MinRequiredVersion = e.MinRequiredVersion,
            MaxAllowedVersion = e.MaxAllowedVersion,
            QaVerifiedAt = e.QaVerifiedAt,
            QaVerifiedByUserId = e.QaVerifiedByUserId,
            QaRemarks = e.QaRemarks,
            ApprovedAt = e.ApprovedAt,
            ApprovedByUserId = e.ApprovedByUserId,
            ApprovalNotes = e.ApprovalNotes,
            RejectedAt = e.RejectedAt,
            RejectedByUserId = e.RejectedByUserId,
            RejectionReason = e.RejectionReason,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt,
            CreatedByUserId = e.CreatedByUserId,
            GiteaAssets = e.GiteaAssets.Select(a => new GiteaAssetItemDto
            {
                AssetId = a.AssetId,
                Name = a.Name,
                DownloadUrl = a.DownloadUrl,
                SizeBytes = a.SizeBytes,
                ContentType = a.ContentType
            }).ToList()
        };
    }
}
