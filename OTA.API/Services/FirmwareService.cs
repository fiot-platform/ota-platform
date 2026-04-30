using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OTA.API.Models.DTOs;
using OTA.API.Models.Entities;
using OTA.API.Models.Enums;
using OTA.API.Models.Settings;
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
        private readonly IProjectRepository _projectRepository;
        private readonly IQASessionRepository _qaSessionRepository;
        private readonly IClientRepository _clientRepository;
        private readonly IGiteaApiService _giteaApiService;
        private readonly GiteaSettings _giteaSettings;
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly IHttpClientFactory _httpClientFactory;
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
            IProjectRepository projectRepository,
            IQASessionRepository qaSessionRepository,
            IClientRepository clientRepository,
            IGiteaApiService giteaApiService,
            IOptions<GiteaSettings> giteaSettings,
            IAuditService auditService,
            INotificationService notificationService,
            IEmailService emailService,
            IHttpClientFactory httpClientFactory,
            ILogger<FirmwareService> logger)
        {
            _firmwareRepository  = firmwareRepository  ?? throw new ArgumentNullException(nameof(firmwareRepository));
            _repoRepository      = repoRepository      ?? throw new ArgumentNullException(nameof(repoRepository));
            _projectRepository   = projectRepository   ?? throw new ArgumentNullException(nameof(projectRepository));
            _qaSessionRepository = qaSessionRepository ?? throw new ArgumentNullException(nameof(qaSessionRepository));
            _clientRepository    = clientRepository    ?? throw new ArgumentNullException(nameof(clientRepository));
            _giteaApiService     = giteaApiService     ?? throw new ArgumentNullException(nameof(giteaApiService));
            _giteaSettings       = giteaSettings?.Value ?? throw new ArgumentNullException(nameof(giteaSettings));
            _auditService        = auditService        ?? throw new ArgumentNullException(nameof(auditService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _emailService        = emailService        ?? throw new ArgumentNullException(nameof(emailService));
            _httpClientFactory   = httpClientFactory   ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger              = logger              ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<FirmwareDto> CreateFirmwareAsync(
            CreateFirmwareRequest request,
            string callerUserId,
            string callerName,
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
                CheckTrial = request.CheckTrial,
                TrialCompleted = false,
                MinRequiredVersion = request.MinRequiredVersion,
                MaxAllowedVersion = request.MaxAllowedVersion,
                CreatedByUserId = callerUserId,
                CreatedByName = !string.IsNullOrWhiteSpace(callerName) ? callerName : callerEmail,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _firmwareRepository.InsertAsync(entity, cancellationToken);

            _ = _emailService.SendCrudNotificationAsync(callerEmail, callerName, "Created", "Firmware", $"v{entity.Version}", CancellationToken.None);

            // ── Commit firmware binary and folder scaffold to Gitea (background) ──
            // Runs fire-and-forget so the API response is returned immediately.
            // Gitea retries can take several minutes; blocking the HTTP request would
            // cause the frontend to time out and show a false "unknown error".
            if (!string.IsNullOrWhiteSpace(request.StoredFileName))
            {
                var storedFileName = request.StoredFileName;
                var originalFileName = request.FileName;
                var entityId = entity.Id;
                var version  = entity.Version.Trim();
                var branch   = !string.IsNullOrWhiteSpace(repo.DefaultBranch) ? repo.DefaultBranch : "main";
                var owner    = repo.GiteaOwner;
                var repoName = repo.GiteaRepoName;
                var giteaBase = _giteaSettings.BaseUrl.TrimEnd('/');

                _ = Task.Run(async () =>
                {
                    try
                    {
                        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "firmware-uploads");
                        var localPath = Path.Combine(uploadDir, storedFileName);

                        if (!File.Exists(localPath))
                        {
                            _logger.LogWarning(
                                "Stored file '{StoredFileName}' not found on disk; Gitea commit skipped.", storedFileName);
                            return;
                        }

                        var fileBytes = await File.ReadAllBytesAsync(localPath);
                        var fileName  = !string.IsNullOrWhiteSpace(originalFileName) ? originalFileName : storedFileName;

                        await _giteaApiService.CreateFileAsync(
                            owner, repoName,
                            $"{version}/{fileName}",
                            $"Add firmware binary for version {version}",
                            fileBytes, branch);

                        var gitkeepBytes = Array.Empty<byte>();
                        foreach (var folder in new[] { "test-case", "test-result", "buglist", "individual" })
                        {
                            await _giteaApiService.CreateFileAsync(
                                owner, repoName,
                                $"{version}/{folder}/.gitkeep",
                                $"Scaffold {folder} folder for version {version}",
                                gitkeepBytes, branch);
                        }

                        var rawUrl = $"{giteaBase}/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repoName)}/raw/branch/{Uri.EscapeDataString(branch)}/{Uri.EscapeDataString(version)}/{Uri.EscapeDataString(fileName)}";

                        // Re-load entity to update DownloadUrl without holding a stale reference
                        var stored = await _firmwareRepository.GetByIdAsync(entityId);
                        if (stored != null)
                        {
                            stored.DownloadUrl = rawUrl;
                            stored.UpdatedAt   = DateTime.UtcNow;
                            await _firmwareRepository.UpdateAsync(stored.Id, stored);
                        }

                        _logger.LogInformation(
                            "Background Gitea commit done for firmware '{Version}' in '{Owner}/{Repo}'.",
                            version, owner, repoName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Background Gitea commit failed for firmware '{Version}'; firmware record is still valid.",
                            version);
                    }
                });
            }

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

            _ = _notificationService.NotifyAsync(
                "Firmware Created",
                $"Firmware version '{entity.Version}' was created in repository '{repo.GiteaRepoName}'.",
                new Dictionary<string, string> { ["type"] = "firmware_created", ["firmwareId"] = entity.FirmwareId, ["version"] = entity.Version },
                cancellationToken: CancellationToken.None);

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

            _ = _notificationService.NotifyAsync(
                "Firmware Updated",
                $"Firmware version '{entity.Version}' was updated.",
                new Dictionary<string, string> { ["type"] = "firmware_updated", ["firmwareId"] = entity.FirmwareId, ["version"] = entity.Version },
                cancellationToken: CancellationToken.None);

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

            _ = _notificationService.NotifyFirmwareApprovedAsync(entity.FirmwareId, entity.Version, CancellationToken.None);

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

            _ = _notificationService.NotifyFirmwareRejectedAsync(entity.FirmwareId, entity.Version, reason, CancellationToken.None);

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

            _ = _notificationService.NotifyAsync(
                "Firmware QA Verified",
                $"Firmware '{entity.Version}' passed QA verification and is pending approval.",
                new Dictionary<string, string> { ["type"] = "firmware_qa_verified", ["firmwareId"] = entity.FirmwareId, ["version"] = entity.Version },
                cancellationToken: CancellationToken.None);

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
            string? repoClientCode = null;
            string? repoClientNameFromRepo = null;
            if (!string.IsNullOrWhiteSpace(entity.RepositoryId))
            {
                var repo = await _repoRepository.GetByIdAsync(entity.RepositoryId, cancellationToken);
                repoName = repo?.GiteaRepoName;
                // ClientCode (e.g. "CUSTOM_00001") is the authoritative link to the client.
                // GiteaOwner (e.g. "Rax_Admin") is just the Gitea org/user — not a client code.
                repoClientCode = repo?.ClientCode;
                repoClientNameFromRepo = repo?.ClientName;
            }

            string? projectName = null;
            if (!string.IsNullOrWhiteSpace(entity.ProjectId))
            {
                var project = MongoDB.Bson.ObjectId.TryParse(entity.ProjectId, out _)
                    ? await _projectRepository.GetByIdAsync(entity.ProjectId, cancellationToken)
                    : await _projectRepository.GetByProjectIdAsync(entity.ProjectId, cancellationToken);
                projectName = project?.Name;
            }

            // Resolve the firmware's owning client. Prefer the repo's denormalised ClientName,
            // else look up the client by code in the clients collection.
            string? clientName = repoClientNameFromRepo;
            if (string.IsNullOrWhiteSpace(clientName) && !string.IsNullOrWhiteSpace(repoClientCode))
            {
                var client = await _clientRepository.GetByCodeAsync(repoClientCode, cancellationToken);
                clientName = client?.Name ?? repoClientCode;
            }

            return MapToDto(entity, repoName, projectName, clientName);
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
            List<string>? allowedProjectIds = null,
            CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var (items, totalCount) = await _firmwareRepository.SearchWithFiltersAsync(
                search, status, channel, projectId, repositoryId, page, pageSize, allowedProjectIds, cancellationToken);

            // Batch-load repository names to avoid N+1 queries
            var repoIds = items.Select(f => f.RepositoryId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
            var repoNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var rid in repoIds)
            {
                var repo = await _repoRepository.GetByIdAsync(rid, cancellationToken);
                if (repo != null) repoNames[rid] = repo.GiteaRepoName;
            }

            // Batch-load project names + client names to avoid N+1 queries
            var projectIds = items.Select(f => f.ProjectId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
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

            // Batch-load QA session statuses for all firmware IDs on this page
            var firmwareIds = items.Select(f => f.FirmwareId).Where(id => !string.IsNullOrWhiteSpace(id)).ToList();
            var qaStatuses = await _qaSessionRepository.GetStatusByFirmwareIdsAsync(firmwareIds, cancellationToken);

            return new PagedResult<FirmwareDto>
            {
                Items = items.Select(e =>
                {
                    var dto = MapToDto(
                        e,
                        repoNames.GetValueOrDefault(e.RepositoryId),
                        projectNames.GetValueOrDefault(e.ProjectId),
                        clientNames.GetValueOrDefault(e.ProjectId));
                    if (qaStatuses.TryGetValue(e.FirmwareId, out var qaStatus))
                        dto.QaSessionStatus = qaStatus;
                    return dto;
                }).ToList(),
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
        public async Task DeleteFirmwareAsync(
            string firmwareId,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(firmwareId)) throw new ArgumentException("FirmwareId is required.", nameof(firmwareId));

            var entity = await _firmwareRepository.GetByFirmwareIdAsync(firmwareId, cancellationToken)
                ?? await _firmwareRepository.GetByIdAsync(firmwareId, cancellationToken)
                ?? throw new KeyNotFoundException($"Firmware '{firmwareId}' not found.");

            // Delete the corresponding Gitea release if one exists.
            if (entity.GiteaReleaseId > 0)
            {
                var repo = await _repoRepository.GetByIdAsync(entity.RepositoryId, cancellationToken);
                if (repo != null && !string.IsNullOrWhiteSpace(repo.GiteaOwner) && !string.IsNullOrWhiteSpace(repo.GiteaRepoName))
                {
                    var deleted = await _giteaApiService.DeleteReleaseAsync(repo.GiteaOwner, repo.GiteaRepoName, entity.GiteaReleaseId, cancellationToken);
                    if (deleted)
                        _logger.LogInformation("Gitea release {ReleaseId} ({Tag}) deleted for firmware '{FirmwareId}'.", entity.GiteaReleaseId, entity.GiteaTagName, firmwareId);
                    else
                        _logger.LogWarning("Gitea release {ReleaseId} not found when deleting firmware '{FirmwareId}' — skipped.", entity.GiteaReleaseId, firmwareId);
                }
            }

            await _firmwareRepository.DeleteAsync(entity.Id, cancellationToken);

            _logger.LogInformation("Firmware '{FirmwareId}' permanently deleted by '{Email}'.", firmwareId, callerEmail);

            await _auditService.LogActionAsync(
                AuditAction.FirmwareDeleted,
                callerUserId, callerEmail, UserRole.SuperAdmin,
                "Firmware", firmwareId,
                JsonSerializer.Serialize(new { entity.Version, entity.Status }, _jsonOptions),
                null,
                ipAddress,
                cancellationToken: cancellationToken);

            _ = _notificationService.NotifyAsync(
                "Firmware Deleted",
                $"Firmware version '{entity.Version}' was permanently deleted.",
                new Dictionary<string, string> { ["type"] = "firmware_deleted", ["firmwareId"] = firmwareId, ["version"] = entity.Version },
                cancellationToken: CancellationToken.None);
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

            _ = _notificationService.NotifyAsync(
                "Firmware Deprecated",
                $"Firmware version '{entity.Version}' was deprecated.",
                new Dictionary<string, string> { ["type"] = "firmware_deprecated", ["firmwareId"] = entity.FirmwareId, ["version"] = entity.Version },
                cancellationToken: CancellationToken.None);
        }

        /// <inheritdoc/>
        public async Task CompleteTrialAsync(
            string firmwareId,
            string remarks,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            var entity = await _firmwareRepository.GetByFirmwareIdAsync(firmwareId, cancellationToken)
                ?? throw new KeyNotFoundException($"Firmware '{firmwareId}' not found.");

            if (!entity.CheckTrial)
                throw new InvalidOperationException($"Firmware '{firmwareId}' does not have Check Trial enabled.");

            if (entity.TrialCompleted)
                throw new InvalidOperationException($"Trial for firmware '{firmwareId}' is already marked complete.");

            entity.TrialCompleted = true;
            entity.TrialCompletedAt = DateTime.UtcNow;
            entity.TrialRemarks = remarks?.Trim();
            entity.UpdatedAt = DateTime.UtcNow;

            await _firmwareRepository.UpdateAsync(entity.Id, entity, cancellationToken);

            _logger.LogInformation(
                "Firmware '{FirmwareId}' trial completed by '{Email}'. Remarks: {Remarks}",
                firmwareId, callerEmail, remarks);

            await _auditService.LogActionAsync(
                AuditAction.FirmwareApproved,
                callerUserId, callerEmail, UserRole.ReleaseManager,
                "Firmware", firmwareId,
                JsonSerializer.Serialize(new { TrialCompleted = false }, _jsonOptions),
                JsonSerializer.Serialize(new { TrialCompleted = true, Remarks = remarks }, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);

            _ = _notificationService.NotifyAsync(
                "Trial OTA Completed",
                $"Firmware v{entity.Version} trial completed. Now eligible for regular OTA.",
                new Dictionary<string, string>
                {
                    ["type"]       = "trial_completed",
                    ["firmwareId"] = entity.FirmwareId,
                    ["version"]    = entity.Version,
                },
                cancellationToken: CancellationToken.None);
        }

        /// <inheritdoc/>
        public async Task<CopyFirmwareToRepositoriesResponse> CopyFirmwareToRepositoriesAsync(
            string parentFirmwareId,
            List<string> targetRepositoryIds,
            string callerUserId,
            string callerName,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(parentFirmwareId))
                throw new ArgumentException("parentFirmwareId is required.", nameof(parentFirmwareId));
            if (targetRepositoryIds == null || targetRepositoryIds.Count == 0)
                throw new ArgumentException("At least one target repository is required.", nameof(targetRepositoryIds));

            var parent = await _firmwareRepository.GetByFirmwareIdAsync(parentFirmwareId, cancellationToken)
                ?? await _firmwareRepository.GetByIdAsync(parentFirmwareId, cancellationToken)
                ?? throw new KeyNotFoundException($"Firmware '{parentFirmwareId}' not found.");

            if (parent.Status != FirmwareStatus.Approved)
                throw new InvalidOperationException("Only Approved firmware can be copied to other repositories.");
            if (string.IsNullOrWhiteSpace(parent.DownloadUrl))
                throw new InvalidOperationException($"Parent firmware v{parent.Version} has no DownloadUrl — cannot fetch its binary.");

            // Download the parent's binary once and reuse the bytes for every target.
            byte[] parentBytes;
            try
            {
                using var http = _httpClientFactory.CreateClient();
                http.Timeout = TimeSpan.FromSeconds(60);
                using var resp = await http.GetAsync(parent.DownloadUrl, cancellationToken);
                resp.EnsureSuccessStatusCode();
                parentBytes = await resp.Content.ReadAsByteArrayAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to fetch parent firmware binary from '{parent.DownloadUrl}': {ex.Message}", ex);
            }

            var fileName = !string.IsNullOrWhiteSpace(parent.FileName)
                ? parent.FileName
                : $"firmware-v{parent.Version}.bin";

            // Resolve client display names for the response, indexed by code AND id.
            var allClients = await _clientRepository.GetAllAsync(cancellationToken);
            var clientNameByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in allClients)
            {
                if (!string.IsNullOrWhiteSpace(c.Code)) clientNameByKey[c.Code] = c.Name;
                if (!string.IsNullOrWhiteSpace(c.Id))   clientNameByKey[c.Id]   = c.Name;
                if (!string.IsNullOrWhiteSpace(c.ClientId)) clientNameByKey[c.ClientId] = c.Name;
            }

            var response = new CopyFirmwareToRepositoriesResponse();

            foreach (var targetRepoId in targetRepositoryIds.Distinct())
            {
                var targetRepo = await _repoRepository.GetByIdAsync(targetRepoId, cancellationToken);

                var result = new CopyFirmwareTargetResult
                {
                    RepositoryId   = targetRepoId,
                    RepositoryName = targetRepo?.GiteaRepoName ?? targetRepoId,
                    ClientName     = ResolveClientNameForRepo(targetRepo, clientNameByKey),
                };

                if (targetRepo == null)
                {
                    result.Status = "failed";
                    result.Reason = "Target repository not found.";
                    response.Results.Add(result);
                    response.FailedCount++;
                    continue;
                }

                // Skip if a firmware row with the same Version already exists in the target repo.
                var existing = await _firmwareRepository.GetByRepositoryIdAsync(targetRepo.Id, cancellationToken);
                if (existing.Any(f => string.Equals(f.Version?.Trim(), parent.Version?.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    result.Status = "skipped";
                    result.Reason = $"v{parent.Version} already exists in {targetRepo.GiteaRepoName}.";
                    response.Results.Add(result);
                    response.SkippedCount++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(targetRepo.GiteaOwner) || string.IsNullOrWhiteSpace(targetRepo.GiteaRepoName))
                {
                    result.Status = "failed";
                    result.Reason = "Target repository is missing Gitea owner or name.";
                    response.Results.Add(result);
                    response.FailedCount++;
                    continue;
                }

                // Re-upload the binary to the target Gitea repo at <version>/<filename>.
                var branch = "main";
                var version = parent.Version.Trim();
                try
                {
                    await _giteaApiService.CreateFileAsync(
                        targetRepo.GiteaOwner, targetRepo.GiteaRepoName,
                        $"{version}/{fileName}",
                        $"Copy firmware v{version} from {parent.RepositoryId} via platform copy",
                        parentBytes, branch, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to upload binary to target Gitea repo '{Owner}/{Repo}' for v{Version}.",
                        targetRepo.GiteaOwner, targetRepo.GiteaRepoName, version);
                    result.Status = "failed";
                    result.Reason = $"Gitea upload failed: {ex.Message}";
                    response.Results.Add(result);
                    response.FailedCount++;
                    continue;
                }

                var giteaBase = _giteaSettings.BaseUrl.TrimEnd('/');
                var rawUrl = $"{giteaBase}/{Uri.EscapeDataString(targetRepo.GiteaOwner)}/{Uri.EscapeDataString(targetRepo.GiteaRepoName)}/raw/branch/{Uri.EscapeDataString(branch)}/{Uri.EscapeDataString(version)}/{Uri.EscapeDataString(fileName)}";

                // Clone the parent record into a new firmware document for the target repo.
                var newRecord = new FirmwareVersionEntity
                {
                    FirmwareId                 = Guid.NewGuid().ToString(),
                    RepositoryId               = targetRepo.Id,
                    ProjectId                  = targetRepo.ProjectId ?? string.Empty,
                    Version                    = version,
                    GiteaTagName               = parent.GiteaTagName,
                    ReleaseNotes               = parent.ReleaseNotes,
                    FileName                   = fileName,
                    FileSha256                 = parent.FileSha256,
                    FileSizeBytes              = parent.FileSizeBytes,
                    DownloadUrl                = rawUrl,
                    Channel                    = parent.Channel,
                    Status                     = FirmwareStatus.Approved,           // Q2 (b)
                    IsMandate                  = parent.IsMandate,
                    SupportedModels            = parent.SupportedModels?.ToList() ?? new List<string>(),
                    SupportedHardwareRevisions = parent.SupportedHardwareRevisions?.ToList() ?? new List<string>(),
                    MinRequiredVersion         = parent.MinRequiredVersion,
                    MaxAllowedVersion          = parent.MaxAllowedVersion,
                    QaVerifiedAt               = parent.QaVerifiedAt,
                    QaVerifiedByUserId         = parent.QaVerifiedByUserId,
                    QaRemarks                  = parent.QaRemarks,
                    ApprovedAt                 = DateTime.UtcNow,
                    ApprovedByUserId           = callerUserId,
                    ApprovalNotes              = $"Copied from {parent.RepositoryId} (firmware {parent.FirmwareId}).",
                    CreatedByUserId            = callerUserId,
                    CreatedByName              = !string.IsNullOrWhiteSpace(callerName) ? callerName : callerEmail,
                    CreatedAt                  = DateTime.UtcNow,
                    UpdatedAt                  = DateTime.UtcNow,
                };

                await _firmwareRepository.InsertAsync(newRecord, cancellationToken);

                result.Status        = "created";
                result.NewFirmwareId = newRecord.FirmwareId;
                response.Results.Add(result);
                response.CreatedCount++;

                await _auditService.LogActionAsync(
                    AuditAction.FirmwareCreated,
                    callerUserId, callerEmail, UserRole.ReleaseManager,
                    "Firmware", newRecord.FirmwareId,
                    null,
                    JsonSerializer.Serialize(new { CopiedFrom = parent.FirmwareId, version, targetRepoId = targetRepo.Id }, _jsonOptions),
                    ipAddress,
                    cancellationToken: cancellationToken);
            }

            _ = _notificationService.NotifyAsync(
                "Firmware Copied",
                $"v{parent.Version} copied: {response.CreatedCount} created, {response.SkippedCount} skipped, {response.FailedCount} failed.",
                new Dictionary<string, string>
                {
                    ["type"]    = "firmware_copied",
                    ["version"] = parent.Version,
                    ["created"] = response.CreatedCount.ToString(),
                    ["skipped"] = response.SkippedCount.ToString(),
                    ["failed"]  = response.FailedCount.ToString(),
                },
                cancellationToken: CancellationToken.None);

            _logger.LogInformation(
                "Firmware v{Version} copied to {Created} repos by '{Email}'. Skipped={Skipped}, Failed={Failed}.",
                parent.Version, response.CreatedCount, callerEmail, response.SkippedCount, response.FailedCount);

            return response;
        }

        private static string ResolveClientNameForRepo(
            Models.Entities.RepositoryMasterEntity? repo,
            Dictionary<string, string> clientNameByKey)
        {
            if (repo == null) return string.Empty;
            // Prefer the denormalised ClientName on the repo, else look up by ClientCode.
            // GiteaOwner is the Gitea org/user — NOT the client code.
            if (!string.IsNullOrWhiteSpace(repo.ClientName)) return repo.ClientName!;
            if (!string.IsNullOrWhiteSpace(repo.ClientCode) &&
                clientNameByKey.TryGetValue(repo.ClientCode, out var n)) return n;
            return repo.ClientCode ?? string.Empty;
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

        private static FirmwareDto MapToDto(FirmwareVersionEntity e, string? repositoryName = null, string? projectName = null, string? clientName = null) => new FirmwareDto
        {
            FirmwareId = e.FirmwareId,
            RepositoryId = e.RepositoryId,
            RepositoryName = repositoryName,
            ProjectId = e.ProjectId,
            ProjectName = projectName,
            ClientName = clientName,
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
            CheckTrial = e.CheckTrial,
            TrialCompleted = e.TrialCompleted,
            TrialCompletedAt = e.TrialCompletedAt,
            TrialRemarks = e.TrialRemarks,
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
            CreatedByName = e.CreatedByName,
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
