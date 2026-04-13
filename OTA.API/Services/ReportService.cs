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
        private readonly ILogger<ReportService> _logger;

        /// <summary>Initialises a new instance of <see cref="ReportService"/>.</summary>
        public ReportService(
            IDeviceRepository deviceRepository,
            IFirmwareRepository firmwareRepository,
            IRolloutRepository rolloutRepository,
            IOtaJobRepository jobRepository,
            IProjectRepository projectRepository,
            IUserRepository userRepository,
            ILogger<ReportService> logger)
        {
            _deviceRepository = deviceRepository ?? throw new ArgumentNullException(nameof(deviceRepository));
            _firmwareRepository = firmwareRepository ?? throw new ArgumentNullException(nameof(firmwareRepository));
            _rolloutRepository = rolloutRepository ?? throw new ArgumentNullException(nameof(rolloutRepository));
            _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
            _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
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

                // Firmware status counts
                var firmwareStatusCounts = await _firmwareRepository.CountByStatusAsync(cancellationToken);

                // Rollout counts
                var activeRollouts = (await _rolloutRepository.GetByStatusAsync(RolloutStatus.Active, cancellationToken)).Count;
                var completedRollouts = (await _rolloutRepository.GetByStatusAsync(RolloutStatus.Completed, cancellationToken)).Count;

                // Project counts
                long totalProjects;
                if (role == UserRole.CustomerAdmin && !string.IsNullOrWhiteSpace(customerId))
                    totalProjects = (await _projectRepository.GetByCustomerIdAsync(customerId, cancellationToken)).Count;
                else
                    totalProjects = await _projectRepository.CountAsync(string.Empty, cancellationToken);

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
                    TotalFirmware = (int)firmwareStatusCounts.Values.Sum(),
                    ApprovedFirmware = (int)firmwareStatusCounts.GetValueOrDefault(FirmwareStatus.Approved),
                    PendingApprovalFirmware = (int)firmwareStatusCounts.GetValueOrDefault(FirmwareStatus.PendingApproval),
                    ActiveRollouts = activeRollouts,
                    CompletedRollouts = completedRollouts,
                    TotalProjects = (int)totalProjects,
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
    }
}
