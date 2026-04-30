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
    /// Orchestrates OTA rollout creation, lifecycle transitions, and job management.
    /// Target device resolution supports AllDevices, DeviceGroup, Site, Channel, and SpecificDevices strategies.
    /// Canary rollouts dispatch an initial batch percentage; policy controls batch size and concurrency.
    /// </summary>
    public class OtaService : IOtaService
    {
        private readonly IRolloutRepository _rolloutRepository;
        private readonly IOtaJobRepository _jobRepository;
        private readonly IDeviceRepository _deviceRepository;
        private readonly IFirmwareRepository _firmwareRepository;
        private readonly IRolloutPolicyRepository _policyRepository;
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;
        private readonly IMqttService _mqttService;
        private readonly ILogger<OtaService> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>Initialises a new instance of <see cref="OtaService"/>.</summary>
        public OtaService(
            IRolloutRepository rolloutRepository,
            IOtaJobRepository jobRepository,
            IDeviceRepository deviceRepository,
            IFirmwareRepository firmwareRepository,
            IRolloutPolicyRepository policyRepository,
            IAuditService auditService,
            INotificationService notificationService,
            IMqttService mqttService,
            ILogger<OtaService> logger)
        {
            _rolloutRepository   = rolloutRepository   ?? throw new ArgumentNullException(nameof(rolloutRepository));
            _jobRepository       = jobRepository       ?? throw new ArgumentNullException(nameof(jobRepository));
            _deviceRepository    = deviceRepository    ?? throw new ArgumentNullException(nameof(deviceRepository));
            _firmwareRepository  = firmwareRepository  ?? throw new ArgumentNullException(nameof(firmwareRepository));
            _policyRepository    = policyRepository    ?? throw new ArgumentNullException(nameof(policyRepository));
            _auditService        = auditService        ?? throw new ArgumentNullException(nameof(auditService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _mqttService         = mqttService         ?? throw new ArgumentNullException(nameof(mqttService));
            _logger              = logger              ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<RolloutDto> CreateRolloutAsync(CreateRolloutRequest request, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.FirmwareId)) throw new ArgumentException("FirmwareId is required.");
            if (string.IsNullOrWhiteSpace(request.ProjectId)) throw new ArgumentException("ProjectId is required.");

            var firmware = await _firmwareRepository.GetByIdAsync(request.FirmwareId, cancellationToken)
                ?? throw new KeyNotFoundException($"Firmware '{request.FirmwareId}' not found.");
            if (firmware.Status != FirmwareStatus.Approved)
                throw new InvalidOperationException($"Only Approved firmware can be used for rollouts. Current: {firmware.Status}.");

            RolloutPolicyEntity? policy = null;
            if (!string.IsNullOrWhiteSpace(request.PolicyId))
                policy = await _policyRepository.GetByIdAsync(request.PolicyId, cancellationToken)
                    ?? throw new KeyNotFoundException($"Policy '{request.PolicyId}' not found.");

            var devices = await ResolveTargetDevicesAsync(request, cancellationToken);
            if (!devices.Any())
                throw new InvalidOperationException("No eligible devices found for the specified target criteria.");

            var canaryPercentage = policy?.CanaryPercentage ?? 100;
            var initialBatchSize = Math.Max(1, (int)Math.Ceiling(devices.Count * canaryPercentage / 100.0));
            var initialBatch = devices.Take(initialBatchSize).ToList();

            var rollout = new RolloutEntity
            {
                Name = request.Name?.Trim() ?? $"Rollout-{firmware.Version}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                Description = request.Description?.Trim(),
                FirmwareId = request.FirmwareId,
                ProjectId = request.ProjectId,
                PolicyId = request.PolicyId,
                TargetType = request.TargetType,
                TargetIds = request.TargetIds ?? new List<string>(),
                Status = RolloutStatus.Draft,
                TotalDeviceCount = devices.Count,
                PendingCount = initialBatchSize,
                SuccessCount = 0,
                FailureCount = 0,
                Phase = RolloutPhase.Canary,
                BatchSize = policy?.BatchSize ?? devices.Count,
                ConcurrencyLimit = policy?.ConcurrencyLimit ?? 10,
                CreatedByUserId = callerUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _rolloutRepository.InsertAsync(rollout, cancellationToken);

            var jobs = initialBatch.Select(d => new OtaJobEntity
            {
                RolloutId = rollout.Id,
                DeviceId = d.Id,
                FirmwareId = request.FirmwareId,
                Status = OtaJobStatus.Pending,
                TargetVersion = firmware.Version,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            await _jobRepository.BulkInsertAsync(jobs, cancellationToken);

            // Mark every targeted device as having an active OTA job so they show up in the
            // Pending tab on the Device OTA screen, stamped with the target firmware version.
            await _deviceRepository.SetActiveOtaJobBulkAsync(
                initialBatch.Select(d => d.Id), true, firmware.Version, cancellationToken);

            _logger.LogInformation("Rollout '{RolloutId}' created for {Count} devices by '{Email}'.", rollout.Id, initialBatchSize, callerEmail);

            await _auditService.LogActionAsync(AuditAction.RolloutCreated, callerUserId, callerEmail, UserRole.SuperAdmin, "Rollout", rollout.Id,
                null, JsonSerializer.Serialize(new { rollout.Id, rollout.FirmwareId, DeviceCount = initialBatchSize }, _jsonOptions),
                ipAddress, cancellationToken: cancellationToken);

            _ = _notificationService.NotifyAsync(
                "Rollout Created",
                $"Rollout '{rollout.Name}' created for {initialBatchSize} device(s).",
                new Dictionary<string, string> { ["type"] = "rollout_created", ["rolloutId"] = rollout.RolloutId, ["name"] = rollout.Name },
                cancellationToken: CancellationToken.None);

            // Publish OTA update metadata to each target device via MQTT
            _ = PublishOtaToDevicesAsync(initialBatch, firmware, cancellationToken);

            return MapRolloutToDto(rollout);
        }

        /// <inheritdoc/>
        public async Task<RolloutDto> StartRolloutAsync(string rolloutId, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default)
        {
            var dto = await TransitionStatusAsync(rolloutId, RolloutStatus.Draft, RolloutStatus.Active, AuditAction.RolloutStarted, callerUserId, callerEmail, ipAddress, cancellationToken);
            _ = _notificationService.NotifyRolloutStartedAsync(dto.RolloutId, dto.Name, dto.ProjectId, CancellationToken.None);
            return dto;
        }

        /// <inheritdoc/>
        public async Task<RolloutDto> PauseRolloutAsync(string rolloutId, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default)
        {
            var dto = await TransitionStatusAsync(rolloutId, RolloutStatus.Active, RolloutStatus.Paused, AuditAction.RolloutPaused, callerUserId, callerEmail, ipAddress, cancellationToken);
            _ = _notificationService.NotifyAsync(
                "Rollout Paused",
                $"Rollout '{dto.Name}' was paused.",
                new Dictionary<string, string> { ["type"] = "rollout_paused", ["rolloutId"] = dto.RolloutId, ["name"] = dto.Name },
                cancellationToken: CancellationToken.None);
            return dto;
        }

        /// <inheritdoc/>
        public async Task<RolloutDto> ResumeRolloutAsync(string rolloutId, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default)
        {
            var dto = await TransitionStatusAsync(rolloutId, RolloutStatus.Paused, RolloutStatus.Active, AuditAction.RolloutResumed, callerUserId, callerEmail, ipAddress, cancellationToken);
            _ = _notificationService.NotifyAsync(
                "Rollout Resumed",
                $"Rollout '{dto.Name}' was resumed.",
                new Dictionary<string, string> { ["type"] = "rollout_resumed", ["rolloutId"] = dto.RolloutId, ["name"] = dto.Name },
                cancellationToken: CancellationToken.None);
            return dto;
        }

        /// <inheritdoc/>
        public async Task<RolloutDto> CancelRolloutAsync(string rolloutId, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default)
        {
            var rollout = await _rolloutRepository.GetByIdAsync(rolloutId, cancellationToken)
                ?? throw new KeyNotFoundException($"Rollout '{rolloutId}' not found.");
            if (rollout.Status == RolloutStatus.Completed || rollout.Status == RolloutStatus.Cancelled)
                throw new InvalidOperationException($"Cannot cancel a rollout in '{rollout.Status}' status.");

            var pendingJobs = await _jobRepository.GetByRolloutIdAsync(rolloutId, cancellationToken);
            foreach (var job in pendingJobs.Where(j => j.Status == OtaJobStatus.Pending))
            {
                job.Status = OtaJobStatus.Cancelled;
                job.UpdatedAt = DateTime.UtcNow;
                await _jobRepository.UpdateAsync(job.Id, job, cancellationToken);
            }

            var oldStatus = rollout.Status;
            rollout.Status = RolloutStatus.Cancelled;
            rollout.UpdatedAt = DateTime.UtcNow;
            await _rolloutRepository.UpdateAsync(rolloutId, rollout, cancellationToken);

            await _auditService.LogActionAsync(AuditAction.RolloutCancelled, callerUserId, callerEmail, UserRole.SuperAdmin, "Rollout", rolloutId,
                JsonSerializer.Serialize(new { Status = oldStatus.ToString() }, _jsonOptions),
                JsonSerializer.Serialize(new { Status = RolloutStatus.Cancelled.ToString() }, _jsonOptions),
                ipAddress, cancellationToken: cancellationToken);

            _ = _notificationService.NotifyRolloutFailedAsync(rollout.RolloutId, rollout.Name, "Cancelled by user", rollout.ProjectId, CancellationToken.None);

            return MapRolloutToDto(rollout);
        }

        /// <inheritdoc/>
        public async Task<RolloutDto?> GetRolloutByIdAsync(string rolloutId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rolloutId)) throw new ArgumentException("RolloutId is required.", nameof(rolloutId));
            var r = await _rolloutRepository.GetByIdAsync(rolloutId, cancellationToken);
            return r == null ? null : MapRolloutToDto(r);
        }

        /// <inheritdoc/>
        public async Task<PagedResult<RolloutDto>> GetRolloutsAsync(string filter, int page, int pageSize, string? projectId = null, CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            var items = await _rolloutRepository.SearchAsync(filter, page, pageSize, projectId, cancellationToken);
            return new PagedResult<RolloutDto>
            {
                Items = items.Select(MapRolloutToDto).ToList(),
                TotalCount = items.Count,
                Page = page,
                PageSize = pageSize
            };
        }

        /// <inheritdoc/>
        public async Task<List<OtaJobDto>> GetRolloutJobsAsync(string rolloutId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rolloutId)) throw new ArgumentException("RolloutId is required.", nameof(rolloutId));
            var jobs = await _jobRepository.GetByRolloutIdAsync(rolloutId, cancellationToken);
            return jobs.Select(MapJobToDto).ToList();
        }

        /// <inheritdoc/>
        public async Task<OtaJobDto> RetryJobAsync(string jobId, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(jobId)) throw new ArgumentException("JobId is required.", nameof(jobId));
            var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken)
                ?? throw new KeyNotFoundException($"OTA job '{jobId}' not found.");
            if (job.Status != OtaJobStatus.Failed)
                throw new InvalidOperationException($"Only Failed jobs can be retried. Current: {job.Status}.");

            job.Status = OtaJobStatus.Pending;
            job.ErrorMessage = null;
            job.RetryCount = (job.RetryCount ?? 0) + 1;
            job.UpdatedAt = DateTime.UtcNow;
            await _jobRepository.UpdateAsync(jobId, job, cancellationToken);

            await _auditService.LogActionAsync(AuditAction.OtaJobRetried, callerUserId, callerEmail, UserRole.SuperAdmin, "OtaJob", jobId,
                JsonSerializer.Serialize(new { Status = OtaJobStatus.Failed.ToString() }, _jsonOptions),
                JsonSerializer.Serialize(new { Status = OtaJobStatus.Pending.ToString() }, _jsonOptions),
                ipAddress, cancellationToken: cancellationToken);

            _ = _notificationService.NotifyAsync(
                "OTA Job Retried",
                $"OTA job '{jobId}' for device '{job.DeviceId}' was retried.",
                new Dictionary<string, string> { ["type"] = "ota_job_retried", ["jobId"] = jobId, ["deviceId"] = job.DeviceId },
                cancellationToken: CancellationToken.None);

            return MapJobToDto(job);
        }

        /// <inheritdoc/>
        public async Task<RolloutSummaryDto> GetRolloutSummaryAsync(string rolloutId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rolloutId)) throw new ArgumentException("RolloutId is required.", nameof(rolloutId));
            var rollout = await _rolloutRepository.GetByIdAsync(rolloutId, cancellationToken)
                ?? throw new KeyNotFoundException($"Rollout '{rolloutId}' not found.");

            var statusCounts = await _jobRepository.CountByStatusAsync(rolloutId, cancellationToken);
            var total = rollout.TotalDeviceCount;
            var succeeded = (int)statusCounts.GetValueOrDefault(OtaJobStatus.Succeeded);
            var failed = (int)statusCounts.GetValueOrDefault(OtaJobStatus.Failed);
            var pending = (int)statusCounts.GetValueOrDefault(OtaJobStatus.Pending);
            var inProgress = (int)statusCounts.GetValueOrDefault(OtaJobStatus.InProgress);
            var cancelled = (int)statusCounts.GetValueOrDefault(OtaJobStatus.Cancelled);

            return new RolloutSummaryDto
            {
                RolloutId = rolloutId,
                RolloutName = rollout.Name,
                Status = rollout.Status.ToString(),
                TotalDevices = total,
                SucceededCount = succeeded,
                FailedCount = failed,
                QueuedCount = pending,
                InProgressCount = inProgress,
                CancelledCount = cancelled
            };
        }

        // ── Private helpers ─────────────────────────────────────────────────────────

        /// <summary>
        /// Publishes OTA firmware metadata to each device in the batch via MQTT.
        /// Topic: OTA/{serialNumber}/Card  (one message per device, fire-and-forget).
        /// </summary>
        private async Task PublishOtaToDevicesAsync(
            List<DeviceEntity> devices,
            FirmwareVersionEntity firmware,
            CancellationToken cancellationToken)
        {
            try
            {
                var payload = new MqttOtaUpdateEnvelope
                {
                    OtaUpdate = new MqttOtaUpdate
                    {
                        Version     = firmware.Version,
                        Description = firmware.ReleaseNotes ?? $"OTA update to version {firmware.Version}",
                        Mandatory   = firmware.IsMandate,
                        Files       =
                        [
                            new MqttFirmwareFile
                            {
                                FileIndex   = 1,
                                DownloadUrl = firmware.DownloadUrl,
                                FileSize    = firmware.FileSizeBytes,
                                Checksum    = new MqttChecksum
                                {
                                    Type  = "SHA256",
                                    Value = firmware.FileSha256
                                }
                            }
                        ]
                    }
                };

                foreach (var device in devices)
                {
                    payload.OtaUpdate.DeviceId = device.SerialNumber;
                    var json  = JsonSerializer.Serialize(payload, _jsonOptions);
                    var topic = MqttTopics.OtaMetadataPublish(device.SerialNumber);

                    await _mqttService.PublishAsync(topic, json, cancellationToken);
                    _logger.LogInformation(
                        "OTA rollout published to device '{Serial}' on topic '{Topic}'.",
                        device.SerialNumber, topic);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MQTT OTA publish failed for rollout — continuing without MQTT delivery.");
            }
        }

        private async Task<List<DeviceEntity>> ResolveTargetDevicesAsync(CreateRolloutRequest request, CancellationToken cancellationToken)
        {
            switch (request.TargetType)
            {
                case TargetType.AllDevices:
                    return await _deviceRepository.GetByStatusAsync(DeviceStatus.Active, cancellationToken);

                case TargetType.Site:
                {
                    var result = new List<DeviceEntity>();
                    foreach (var id in request.TargetIds ?? new List<string>())
                        result.AddRange((await _deviceRepository.GetBySiteIdAsync(id, cancellationToken)).Where(d => d.Status == DeviceStatus.Active));
                    return result.DistinctBy(d => d.Id).ToList();
                }

                case TargetType.Channel:
                {
                    var result = new List<DeviceEntity>();
                    foreach (var channelName in request.TargetIds ?? new List<string>())
                    {
                        if (Enum.TryParse<FirmwareChannel>(channelName, out var ch))
                        {
                            var all = await _deviceRepository.GetByStatusAsync(DeviceStatus.Active, cancellationToken);
                            result.AddRange(all.Where(d => d.FirmwareChannel == ch));
                        }
                    }
                    return result.DistinctBy(d => d.Id).ToList();
                }

                case TargetType.SpecificDevices:
                {
                    var result = new List<DeviceEntity>();
                    foreach (var id in request.TargetIds ?? new List<string>())
                    {
                        var d = await _deviceRepository.GetByIdAsync(id, cancellationToken);
                        if (d?.Status == DeviceStatus.Active) result.Add(d);
                    }
                    return result;
                }

                case TargetType.DeviceGroup:
                {
                    var result = new List<DeviceEntity>();
                    foreach (var customerId in request.TargetIds ?? new List<string>())
                        result.AddRange((await _deviceRepository.GetByCustomerIdAsync(customerId, cancellationToken)).Where(d => d.Status == DeviceStatus.Active));
                    return result.DistinctBy(d => d.Id).ToList();
                }

                default:
                    throw new ArgumentException($"Unsupported TargetType: {request.TargetType}.");
            }
        }

        private async Task<RolloutDto> TransitionStatusAsync(
            string rolloutId, RolloutStatus from, RolloutStatus to,
            AuditAction action, string callerUserId, string callerEmail, string ipAddress, CancellationToken cancellationToken)
        {
            var rollout = await _rolloutRepository.GetByIdAsync(rolloutId, cancellationToken)
                ?? throw new KeyNotFoundException($"Rollout '{rolloutId}' not found.");
            if (rollout.Status != from)
                throw new InvalidOperationException($"Expected status '{from}' but found '{rollout.Status}'.");

            rollout.Status = to;
            rollout.UpdatedAt = DateTime.UtcNow;
            await _rolloutRepository.UpdateAsync(rolloutId, rollout, cancellationToken);

            await _auditService.LogActionAsync(action, callerUserId, callerEmail, UserRole.SuperAdmin, "Rollout", rolloutId,
                JsonSerializer.Serialize(new { Status = from.ToString() }, _jsonOptions),
                JsonSerializer.Serialize(new { Status = to.ToString() }, _jsonOptions),
                ipAddress, cancellationToken: cancellationToken);

            return MapRolloutToDto(rollout);
        }

        private static RolloutDto MapRolloutToDto(RolloutEntity r) => new RolloutDto
        {
            Id = r.Id,
            RolloutId = r.RolloutId,
            Name = r.Name,
            Description = r.Description,
            FirmwareId = r.FirmwareId,
            ProjectId = r.ProjectId,
            PolicyId = r.PolicyId,
            TargetType = r.TargetType.ToString(),
            TargetIds = r.TargetIds,
            Status = r.Status.ToString(),
            Phase = r.Phase.ToString(),
            TotalDeviceCount = r.TotalDevices,
            SuccessCount = r.SucceededCount,
            FailureCount = r.FailedCount,
            CreatedByUserId = r.CreatedByUserId,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        };

        private static OtaJobDto MapJobToDto(OtaJobEntity j) => new OtaJobDto
        {
            Id = j.Id,
            JobId = j.JobId,
            RolloutId = j.RolloutId,
            DeviceId = j.DeviceId,
            FirmwareId = j.FirmwareId,
            Status = j.Status.ToString(),
            TargetVersion = j.TargetVersion,
            RetryCount = j.RetryCount ?? 0,
            ErrorMessage = j.ErrorMessage,
            CompletedAt = j.CompletedAt,
            CreatedAt = j.CreatedAt,
            UpdatedAt = j.UpdatedAt
        };
    }
}
