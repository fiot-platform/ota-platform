using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Implements dashboard and reporting aggregations across the OTA platform.
    /// Applies customer scoping for CustomerAdmin callers and returns pre-aggregated metrics.
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly IFirmwareRepository _firmwareRepository;
        private readonly IRolloutRepository _rolloutRepository;
        private readonly IOtaJobRepository _jobRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRepositoryMasterRepository _repositoryRepository;
        private readonly IClientRepository _clientRepository;
        private readonly ILogger<ReportService> _logger;

        /// <summary>Initialises a new instance of <see cref="ReportService"/>.</summary>
        public ReportService(
            IDeviceRepository deviceRepository,
            IFirmwareRepository firmwareRepository,
            IRolloutRepository rolloutRepository,
            IOtaJobRepository jobRepository,
            IProjectRepository projectRepository,
            IUserRepository userRepository,
            IRepositoryMasterRepository repositoryRepository,
            IClientRepository clientRepository,
            ILogger<ReportService> logger)
        {
            _deviceRepository = deviceRepository ?? throw new ArgumentNullException(nameof(deviceRepository));
            _firmwareRepository = firmwareRepository ?? throw new ArgumentNullException(nameof(firmwareRepository));
            _rolloutRepository = rolloutRepository ?? throw new ArgumentNullException(nameof(rolloutRepository));
            _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
            _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _repositoryRepository = repositoryRepository ?? throw new ArgumentNullException(nameof(repositoryRepository));
            _clientRepository = clientRepository ?? throw new ArgumentNullException(nameof(clientRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(
            string userId,
            UserRole role,
            string? customerId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Device counts (scoped to customer for CustomerAdmin)
                List<DeviceEntity> devices;
                if (role == UserRole.CustomerAdmin && !string.IsNullOrWhiteSpace(customerId))
                    devices = await _deviceRepository.GetByCustomerIdAsync(customerId, cancellationToken);
                else
                    devices = await _deviceRepository.GetAllAsync(cancellationToken);

                var totalDevices = devices.Count;
                var activeDevices = devices.Count(d => d.Status == DeviceStatus.Active);
                var suspendedDevices = devices.Count(d => d.Status == DeviceStatus.Suspended);
                var offlineDevices = devices.Count(d => d.LastSeen < DateTime.UtcNow.AddMinutes(-30));
                var devicesUpdating = devices.Count(d =>
                    d.OtaStatus == "start" || d.OtaStatus == "inprogress");

                // Firmware status counts — group in memory to avoid BsonNull issues in CountByStatusAsync aggregation
                var allFirmwareForCounts = await _firmwareRepository.GetAllAsync(cancellationToken);
                var firmwareStatusCounts = allFirmwareForCounts
                    .GroupBy(f => f.Status)
                    .ToDictionary(g => g.Key, g => (long)g.Count());

                // Rollout counts
                var activeRollouts = (await _rolloutRepository.GetByStatusAsync(RolloutStatus.Active, cancellationToken)).Count;
                var completedRollouts = (await _rolloutRepository.GetByStatusAsync(RolloutStatus.Completed, cancellationToken)).Count;

                // Project counts
                long totalProjects;
                if (role == UserRole.CustomerAdmin && !string.IsNullOrWhiteSpace(customerId))
                    totalProjects = (await _projectRepository.GetByCustomerIdAsync(customerId, cancellationToken)).Count;
                else
                    totalProjects = await _projectRepository.CountAsync(string.Empty, null, cancellationToken);

                // Repository counts
                var allReposForCount = await _repositoryRepository.GetAllAsync(cancellationToken);
                long totalRepositories = allReposForCount.Count;

                // User counts (SuperAdmin only view)
                long totalUsers = 0;
                if (role == UserRole.SuperAdmin)
                    totalUsers = await _userRepository.CountAsync(string.Empty, cancellationToken);

                return new DashboardSummaryDto
                {
                    TotalDevices = totalDevices,
                    ActiveDevices = activeDevices,
                    SuspendedDevices = suspendedDevices,
                    OfflineDevices = offlineDevices,
                    DevicesUpdating = devicesUpdating,
                    TotalFirmware = (int)firmwareStatusCounts.Values.Sum(),
                    ApprovedFirmware = (int)firmwareStatusCounts.GetValueOrDefault(FirmwareStatus.Approved),
                    PendingApprovalFirmware = (int)firmwareStatusCounts.GetValueOrDefault(FirmwareStatus.PendingApproval),
                    PendingQAFirmware = (int)firmwareStatusCounts.GetValueOrDefault(FirmwareStatus.PendingQA),
                    ActiveRollouts = activeRollouts,
                    CompletedRollouts = completedRollouts,
                    TotalProjects = (int)totalProjects,
                    TotalRepositories = (int)totalRepositories,
                    TotalUsers = (int)totalUsers,
                    GeneratedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate dashboard summary for user '{UserId}'.", userId);
                throw new InvalidOperationException("Failed to generate dashboard summary.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<FirmwareApprovalTrendDto>> GetFirmwareApprovalTrendAsync(int days, CancellationToken cancellationToken = default)
        {
            if (days < 1) days = 30;

            try
            {
                var allFirmware = await _firmwareRepository.GetAllAsync(cancellationToken);
                var fromDate = DateTime.UtcNow.Date.AddDays(-days);

                var trend = new List<FirmwareApprovalTrendDto>();
                for (int i = 0; i <= days; i++)
                {
                    var date = fromDate.AddDays(i);
                    var approved  = allFirmware.Count(f => f.ApprovedAt.HasValue && f.ApprovedAt.Value.Date == date);
                    var rejected  = allFirmware.Count(f => f.Status == FirmwareStatus.Rejected && f.UpdatedAt.Date == date);
                    var submitted = allFirmware.Count(f => f.CreatedAt.Date == date);

                    trend.Add(new FirmwareApprovalTrendDto
                    {
                        Date      = date,
                        Approved  = approved,
                        Rejected  = rejected,
                        Submitted = submitted
                    });
                }

                return trend;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compute firmware approval trend for {Days} days.", days);
                throw new InvalidOperationException("Failed to compute firmware approval trend.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<RolloutSuccessRateDto>> GetRolloutSuccessRateAsync(string? projectId, CancellationToken cancellationToken = default)
        {
            try
            {
                List<ProjectEntity> projects;
                if (!string.IsNullOrWhiteSpace(projectId))
                {
                    var p = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
                    projects = p != null ? new List<ProjectEntity> { p } : new List<ProjectEntity>();
                }
                else
                {
                    projects = await _projectRepository.GetAllAsync(cancellationToken);
                }

                var result = new List<RolloutSuccessRateDto>();

                foreach (var project in projects)
                {
                    var rollouts = await _rolloutRepository.GetByProjectIdAsync(project.Id, cancellationToken);
                    if (rollouts.Count == 0) continue;

                    var successful = rollouts.Count(r => r.Status == RolloutStatus.Completed);
                    var failed     = rollouts.Count(r => r.Status == RolloutStatus.Failed || r.Status == RolloutStatus.Cancelled);
                    var total      = rollouts.Count;
                    var rate       = total > 0 ? Math.Round((double)successful / total * 100, 1) : 0;

                    result.Add(new RolloutSuccessRateDto
                    {
                        ProjectId          = project.Id,
                        ProjectName        = project.Name,
                        TotalRollouts      = total,
                        SuccessfulRollouts = successful,
                        FailedRollouts     = failed,
                        SuccessRate        = rate
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compute rollout success rate.");
                throw new InvalidOperationException("Failed to compute rollout success rate.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<DeviceUpdateStatusDto>> GetDeviceUpdateStatusAsync(string? customerId, CancellationToken cancellationToken = default)
        {
            try
            {
                var pendingJobs    = await _jobRepository.GetByStatusAsync(OtaJobStatus.Pending,    cancellationToken);
                var inProgressJobs = await _jobRepository.GetByStatusAsync(OtaJobStatus.InProgress, cancellationToken);
                var failedJobs     = await _jobRepository.GetByStatusAsync(OtaJobStatus.Failed,     cancellationToken);

                var pendingDeviceIds    = pendingJobs.Select(j => j.DeviceId).ToHashSet();
                var inProgressDeviceIds = inProgressJobs.Select(j => j.DeviceId).ToHashSet();
                var failedDeviceIds     = failedJobs.Select(j => j.DeviceId).ToHashSet();

                List<DeviceEntity> devices;
                if (!string.IsNullOrWhiteSpace(customerId))
                    devices = await _deviceRepository.GetByCustomerIdAsync(customerId, cancellationToken);
                else
                    devices = await _deviceRepository.GetAllAsync(cancellationToken);

                DeviceUpdateStatusDto BuildStatus(IEnumerable<DeviceEntity> devs, string? cid, string? cname)
                {
                    var list     = devs.ToList();
                    var ids      = list.Select(d => d.Id).ToHashSet();
                    var offline  = list.Count(d => d.LastSeen < DateTime.UtcNow.AddMinutes(-30));
                    var updating = ids.Count(id => inProgressDeviceIds.Contains(id));
                    var avail    = ids.Count(id => pendingDeviceIds.Contains(id) && !inProgressDeviceIds.Contains(id));
                    var fail     = ids.Count(id => failedDeviceIds.Contains(id));
                    var upToDate = Math.Max(0, list.Count - updating - avail - fail);

                    return new DeviceUpdateStatusDto
                    {
                        CustomerId    = cid,
                        CustomerName  = cname,
                        Total         = list.Count,
                        UpToDate      = upToDate,
                        UpdateAvailable = avail,
                        Updating      = updating,
                        Failed        = fail,
                        Offline       = offline,
                        GeneratedAt   = DateTime.UtcNow
                    };
                }

                if (!string.IsNullOrWhiteSpace(customerId))
                {
                    return new List<DeviceUpdateStatusDto> { BuildStatus(devices, customerId, null) };
                }

                // Platform-wide: group by customer
                var byCustomer = devices
                    .GroupBy(d => d.CustomerId ?? string.Empty)
                    .ToList();

                if (!byCustomer.Any())
                    return new List<DeviceUpdateStatusDto> { BuildStatus(devices, null, "All Customers") };

                var results = new List<DeviceUpdateStatusDto>();
                foreach (var group in byCustomer)
                {
                    results.Add(BuildStatus(group, string.IsNullOrEmpty(group.Key) ? null : group.Key,
                        string.IsNullOrEmpty(group.Key) ? "Unassigned" : group.Key));
                }
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get device update status.");
                throw new InvalidOperationException("Failed to get device update status.", ex);
            }
        }

        // ── Extended report implementations ───────────────────────────────────

        /// <inheritdoc/>
        public async Task<List<UserReportDto>> GetUsersReportAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var users = await _userRepository.GetAllAsync(cancellationToken);
                return users.Select(u => new UserReportDto
                {
                    UserId      = u.UserId,
                    Name        = u.Name,
                    Email       = u.Email,
                    Role        = u.Role.ToString(),
                    CustomerId  = u.CustomerId,
                    IsActive    = u.IsActive,
                    LastLoginAt = u.LastLoginAt,
                    CreatedAt   = u.CreatedAt
                }).OrderBy(u => u.Name).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate users report.");
                throw new InvalidOperationException("Failed to generate users report.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<ProjectReportDto>> GetProjectsReportAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var projects = await _projectRepository.GetAllAsync(cancellationToken);
                var allRepos = await _repositoryRepository.GetAllAsync(cancellationToken);
                var allFirmware = await _firmwareRepository.GetAllAsync(cancellationToken);
                var activeRollouts = await _rolloutRepository.GetByStatusAsync(RolloutStatus.Active, cancellationToken);

                var repoCountByProject = allRepos
                    .GroupBy(r => r.ProjectId)
                    .ToDictionary(g => g.Key, g => g.Count());

                var firmwareCountByProject = allFirmware
                    .GroupBy(f => f.ProjectId)
                    .ToDictionary(g => g.Key, g => g.Count());

                var activeRolloutCountByProject = activeRollouts
                    .GroupBy(r => r.ProjectId)
                    .ToDictionary(g => g.Key, g => g.Count());

                return projects.Select(p => new ProjectReportDto
                {
                    ProjectId          = p.ProjectId,
                    Name               = p.Name,
                    CustomerId         = p.CustomerId,
                    CustomerName       = p.CustomerName,
                    RepositoryCount    = repoCountByProject.GetValueOrDefault(p.ProjectId),
                    FirmwareCount      = firmwareCountByProject.GetValueOrDefault(p.ProjectId),
                    ActiveRolloutCount = activeRolloutCountByProject.GetValueOrDefault(p.ProjectId),
                    IsActive           = p.IsActive,
                    CreatedAt          = p.CreatedAt
                }).OrderBy(p => p.Name).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate projects report.");
                throw new InvalidOperationException("Failed to generate projects report.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<RepositoryReportDto>> GetRepositoriesReportAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var repos = await _repositoryRepository.GetAllAsync(cancellationToken);
                var projects = await _projectRepository.GetAllAsync(cancellationToken);
                var allFirmware = await _firmwareRepository.GetAllAsync(cancellationToken);

                var projectEntityMap = projects.ToDictionary(p => p.ProjectId, p => p);
                var firmwareCountByRepo = allFirmware
                    .GroupBy(f => f.RepositoryId)
                    .ToDictionary(g => g.Key, g => g.Count());

                return repos.Select(r =>
                {
                    var project = projectEntityMap.GetValueOrDefault(r.ProjectId);
                    return new RepositoryReportDto
                    {
                        RepositoryId      = r.RepositoryId,
                        Name              = r.GiteaRepoName,
                        ProjectId         = r.ProjectId,
                        ProjectName       = project?.Name ?? r.ProjectId,
                        ClientName        = project?.CustomerName,
                        FirmwareCount     = firmwareCountByRepo.GetValueOrDefault(r.RepositoryId),
                        WebhookConfigured = r.WebhookConfigured,
                        LastSyncedAt      = r.LastSyncedAt,
                        IsActive          = r.IsActive,
                        CreatedAt         = r.CreatedAt
                    };
                }).OrderBy(r => r.Name).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate repositories report.");
                throw new InvalidOperationException("Failed to generate repositories report.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<FirmwareVersionReportDto>> GetFirmwareVersionsReportAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var firmware = await _firmwareRepository.GetAllAsync(cancellationToken);
                var projects = await _projectRepository.GetAllAsync(cancellationToken);
                var repos    = await _repositoryRepository.GetAllAsync(cancellationToken);
                var users    = await _userRepository.GetAllAsync(cancellationToken);
                var devices  = await _deviceRepository.GetAllAsync(cancellationToken);

                var projectMap = projects.ToDictionary(p => p.ProjectId, p => p.Name);
                // firmware.RepositoryId can hold either the platform RepositoryId or the
                // MongoDB _id (legacy data). Index by both so the name lookup succeeds.
                var repoMap    = new Dictionary<string, string>();
                foreach (var r in repos)
                {
                    if (!string.IsNullOrWhiteSpace(r.RepositoryId)) repoMap[r.RepositoryId] = r.GiteaRepoName;
                    if (!string.IsNullOrWhiteSpace(r.Id))           repoMap[r.Id]           = r.GiteaRepoName;
                }
                var userMap    = users.ToDictionary(u => u.UserId, u => u.Name);

                // Count devices per firmware version string, grouped by version
                var deviceCountByVersion = devices
                    .Where(d => d.CurrentFirmwareVersion != null)
                    .GroupBy(d => d.CurrentFirmwareVersion!)
                    .ToDictionary(g => g.Key, g => g.Count());

                return firmware.Select(f => new FirmwareVersionReportDto
                {
                    FirmwareId       = f.FirmwareId,
                    Version          = f.Version,
                    ProjectId        = f.ProjectId,
                    ProjectName      = projectMap.GetValueOrDefault(f.ProjectId, f.ProjectId),
                    RepositoryId     = f.RepositoryId,
                    RepositoryName   = repoMap.GetValueOrDefault(f.RepositoryId, f.RepositoryId),
                    Channel          = f.Channel.ToString(),
                    Status           = f.Status.ToString(),
                    FileSizeBytes    = f.FileSizeBytes,
                    CreatedByName    = !string.IsNullOrWhiteSpace(f.CreatedByName)
                                        ? f.CreatedByName
                                        : (f.CreatedByUserId != null ? userMap.GetValueOrDefault(f.CreatedByUserId) : null),
                    CreatedAt        = f.CreatedAt,
                    QaVerifiedByName = f.QaVerifiedByUserId != null ? userMap.GetValueOrDefault(f.QaVerifiedByUserId) : null,
                    QaVerifiedAt     = f.QaVerifiedAt,
                    ApprovedByName   = f.ApprovedByUserId != null ? userMap.GetValueOrDefault(f.ApprovedByUserId) : null,
                    ApprovedAt       = f.ApprovedAt,
                    DeviceCount      = deviceCountByVersion.GetValueOrDefault(f.Version),
                }).OrderByDescending(f => f.CreatedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate firmware versions report.");
                throw new InvalidOperationException("Failed to generate firmware versions report.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<DeviceReportDto>> GetDevicesReportAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var devices = await _deviceRepository.GetAllAsync(cancellationToken);
                var clients = await _clientRepository.GetAllAsync(cancellationToken);
                var clientNameByCode = clients.ToDictionary(c => c.Code, c => c.Name, StringComparer.OrdinalIgnoreCase);

                return devices.Select(d => new DeviceReportDto
                {
                    DeviceId               = d.DeviceId,
                    SerialNumber           = d.SerialNumber,
                    Name                   = d.SiteName,
                    MacImeiIp              = d.MacImeiIp ?? d.SerialNumber,
                    CustomerName           = clientNameByCode.TryGetValue(d.CustomerName ?? d.CustomerId ?? string.Empty, out var n)
                                                ? n
                                                : (string.IsNullOrWhiteSpace(d.CustomerName) ? d.CustomerId : d.CustomerName),
                    Model                  = d.Model,
                    ProjectId              = d.ProjectId ?? string.Empty,
                    ProjectName            = d.ProjectName ?? string.Empty,
                    CurrentFirmwareVersion = d.CurrentFirmwareVersion,
                    Status                 = d.Status.ToString(),
                    LastHeartbeatAt        = d.LastHeartbeatAt,
                    RegisteredAt           = d.RegisteredAt,
                    LastOtaAt              = d.OtaUpdatedAt
                }).OrderBy(d => d.SerialNumber).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate devices report.");
                throw new InvalidOperationException("Failed to generate devices report.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<ProjectRepoFirmwareRowDto>> GetProjectRepoFirmwareReportAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var projects  = await _projectRepository.GetAllAsync(cancellationToken);
                var repos     = await _repositoryRepository.GetAllAsync(cancellationToken);
                var firmware  = await _firmwareRepository.GetAllAsync(cancellationToken);
                var users     = await _userRepository.GetAllAsync(cancellationToken);
                var devices   = await _deviceRepository.GetAllAsync(cancellationToken);

                var projectMap = projects.ToDictionary(p => p.ProjectId);
                // firmware.RepositoryId may hold either the platform RepositoryId or the
                // MongoDB _id (legacy data). Index by both so lookups succeed.
                var repoMap    = new Dictionary<string, RepositoryMasterEntity>();
                foreach (var r in repos)
                {
                    if (!string.IsNullOrWhiteSpace(r.RepositoryId)) repoMap[r.RepositoryId] = r;
                    if (!string.IsNullOrWhiteSpace(r.Id))           repoMap[r.Id]           = r;
                }
                var userMap = users.ToDictionary(u => u.UserId, u => u.Name);
                var deviceCountByVersion = devices
                    .Where(d => d.CurrentFirmwareVersion != null)
                    .GroupBy(d => d.CurrentFirmwareVersion!)
                    .ToDictionary(g => g.Key, g => g.Count());

                return firmware.Select(f =>
                {
                    projectMap.TryGetValue(f.ProjectId, out var proj);
                    repoMap.TryGetValue(f.RepositoryId, out var repo);
                    return new ProjectRepoFirmwareRowDto
                    {
                        ProjectId        = f.ProjectId,
                        ProjectName      = proj?.Name ?? f.ProjectId,
                        CustomerName     = proj?.CustomerName ?? string.Empty,
                        RepositoryId     = f.RepositoryId,
                        RepositoryName   = repo?.GiteaRepoName ?? f.RepositoryId,
                        FirmwareId       = f.FirmwareId,
                        FirmwareVersion  = f.Version,
                        Channel          = f.Channel.ToString(),
                        FirmwareStatus   = f.Status.ToString(),
                        SupportedModels  = f.SupportedModels ?? new List<string>(),
                        FileSizeBytes    = f.FileSizeBytes,
                        CreatedByName    = !string.IsNullOrWhiteSpace(f.CreatedByName)
                                            ? f.CreatedByName
                                            : (f.CreatedByUserId != null ? userMap.GetValueOrDefault(f.CreatedByUserId) : null),
                        FirmwareCreatedAt = f.CreatedAt,
                        QaVerifiedByName = f.QaVerifiedByUserId != null ? userMap.GetValueOrDefault(f.QaVerifiedByUserId) : null,
                        QaVerifiedAt     = f.QaVerifiedAt,
                        ApprovedByName   = f.ApprovedByUserId != null ? userMap.GetValueOrDefault(f.ApprovedByUserId) : null,
                        ApprovedAt       = f.ApprovedAt,
                        DeviceCount      = deviceCountByVersion.GetValueOrDefault(f.Version)
                    };
                }).OrderBy(r => r.ProjectName).ThenBy(r => r.RepositoryName).ThenBy(r => r.FirmwareVersion).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate project-repo-firmware report.");
                throw new InvalidOperationException("Failed to generate project-repo-firmware report.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<DeviceOtaHistoryRowDto>> GetDeviceOtaHistoryAsync(string? deviceId, CancellationToken cancellationToken = default)
        {
            try
            {
                List<OtaJobEntity> jobs;
                if (!string.IsNullOrWhiteSpace(deviceId))
                    jobs = await _jobRepository.GetByDeviceIdAsync(deviceId, cancellationToken);
                else
                    jobs = await _jobRepository.GetAllAsync(cancellationToken);

                var devices = await _deviceRepository.GetAllAsync(cancellationToken);
                var clients = await _clientRepository.GetAllAsync(cancellationToken);
                // Map client code → real client name. Existing devices stored CustomerCode in
                // CustomerName by mistake; translate it back to a human-readable name here.
                var clientNameByCode = clients.ToDictionary(c => c.Code, c => c.Name, StringComparer.OrdinalIgnoreCase);
                // Match jobs by either the platform DeviceId (GUID) or the MongoDB _id —
                // different code paths use different identifiers when creating jobs.
                var deviceMap = new Dictionary<string, DeviceEntity>();
                foreach (var d in devices)
                {
                    if (!string.IsNullOrWhiteSpace(d.DeviceId)) deviceMap[d.DeviceId] = d;
                    if (!string.IsNullOrWhiteSpace(d.Id))       deviceMap[d.Id]       = d;
                }

                string? ResolveClientName(DeviceEntity? d)
                {
                    if (d == null) return null;
                    var stored = d.CustomerName;
                    if (string.IsNullOrWhiteSpace(stored)) stored = d.CustomerId;
                    if (string.IsNullOrWhiteSpace(stored)) return null;
                    // If what we have is a known code, swap it for the actual name.
                    if (clientNameByCode.TryGetValue(stored, out var name)) return name;
                    return stored;
                }

                // For each device, compute the firmware version that was running just before
                // each succeeded OTA — the prior succeeded job's FirmwareVersion in chronological order.
                var prevByJob = new Dictionary<string, string?>();
                foreach (var deviceJobs in jobs.GroupBy(j => j.DeviceId))
                {
                    string? lastSucceededVersion = null;
                    foreach (var j in deviceJobs.OrderBy(j => j.CreatedAt))
                    {
                        prevByJob[j.JobId] = lastSucceededVersion;
                        if (j.Status == OtaJobStatus.Succeeded && !string.IsNullOrWhiteSpace(j.FirmwareVersion))
                            lastSucceededVersion = j.FirmwareVersion;
                    }
                }

                return jobs.Select(j =>
                {
                    deviceMap.TryGetValue(j.DeviceId, out var device);
                    prevByJob.TryGetValue(j.JobId, out var oldVersion);
                    return new DeviceOtaHistoryRowDto
                    {
                        DeviceId           = j.DeviceId,
                        DeviceSerial       = j.DeviceSerialNumber,
                        DeviceName         = device?.SiteName,
                        MacImeiIp          = device?.MacImeiIp ?? device?.SerialNumber,
                        CustomerName       = ResolveClientName(device),
                        Model              = device?.Model ?? string.Empty,
                        ProjectName            = device?.ProjectName ?? string.Empty,
                        RepositoryName         = device?.RepositoryName,
                        OldFirmwareVersion     = oldVersion,
                        FirmwareVersion        = j.FirmwareVersion,
                        CurrentFirmwareVersion = device?.CurrentFirmwareVersion,
                        PendingFirmwareVersion = device?.PendingFirmwareVersion,
                        JobStatus              = j.Status.ToString(),
                        OtaStatus              = device?.OtaStatus,
                        OtaProgress            = device?.OtaProgress ?? 0,
                        DeviceStatus           = device?.Status.ToString() ?? string.Empty,
                        LastHeartbeatAt        = device?.LastHeartbeatAt,
                        StartedAt              = j.StartedAt,
                        CompletedAt            = j.CompletedAt,
                        PushedAt               = j.AcknowledgedAt ?? j.CreatedAt,
                        PushedByName           = j.AcknowledgedByName
                    };
                }).OrderByDescending(r => r.PushedAt ?? r.StartedAt ?? r.CompletedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate device OTA history report.");
                throw new InvalidOperationException("Failed to generate device OTA history report.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<DailyOtaProgressDto>> GetDailyOtaProgressAsync(int days, CancellationToken cancellationToken = default)
        {
            if (days < 1) days = 14;

            try
            {
                var allJobs = await _jobRepository.GetAllAsync(cancellationToken);
                var fromDate = DateTime.UtcNow.Date.AddDays(-days + 1);

                var result = new List<DailyOtaProgressDto>();
                for (int i = 0; i < days; i++)
                {
                    var date = fromDate.AddDays(i);
                    var dayJobs = allJobs.Where(j => j.CreatedAt.Date == date).ToList();

                    result.Add(new DailyOtaProgressDto
                    {
                        Date       = date.ToString("yyyy-MM-dd"),
                        Total      = dayJobs.Count,
                        Succeeded  = dayJobs.Count(j => j.Status == OtaJobStatus.Succeeded),
                        Failed     = dayJobs.Count(j => j.Status == OtaJobStatus.Failed),
                        InProgress = dayJobs.Count(j => j.Status == OtaJobStatus.InProgress),
                        Queued     = dayJobs.Count(j => j.Status == OtaJobStatus.Queued || j.Status == OtaJobStatus.Created),
                        Cancelled  = dayJobs.Count(j => j.Status == OtaJobStatus.Cancelled)
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate daily OTA progress report for {Days} days.", days);
                throw new InvalidOperationException("Failed to generate daily OTA progress report.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<List<FirmwareStageReportDto>> GetFirmwareStageReportAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Group in memory to avoid BsonNull issues in CountByStatusAsync aggregation
                var allFirmware = await _firmwareRepository.GetAllAsync(cancellationToken);
                var total = allFirmware.Count;

                return allFirmware
                    .GroupBy(f => f.Status)
                    .Select(g => new FirmwareStageReportDto
                    {
                        Stage      = g.Key.ToString(),
                        Count      = g.Count(),
                        Percentage = total > 0 ? Math.Round((double)g.Count() / total * 100, 1) : 0
                    })
                    .OrderByDescending(s => s.Count)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate firmware stage report.");
                throw new InvalidOperationException("Failed to generate firmware stage report.", ex);
            }
        }
    }
}
