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
    /// Implements device registration, heartbeat processing, OTA update eligibility checks,
    /// and device lifecycle state transitions.
    /// </summary>
    public class DeviceService : IDeviceService
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly IFirmwareRepository _firmwareRepository;
        private readonly IOtaJobRepository _jobRepository;
        private readonly IDeviceOtaEventRepository _otaEventRepository;
        private readonly IVersionComparisonService _versionService;
        private readonly IAuditService _auditService;
        private readonly INotificationService _notificationService;
        private readonly IMqttService _mqttService;
        private readonly ILogger<DeviceService> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Initialises a new instance of <see cref="DeviceService"/>.
        /// </summary>
        public DeviceService(
            IDeviceRepository deviceRepository,
            IFirmwareRepository firmwareRepository,
            IOtaJobRepository jobRepository,
            IDeviceOtaEventRepository otaEventRepository,
            IVersionComparisonService versionService,
            IAuditService auditService,
            INotificationService notificationService,
            IMqttService mqttService,
            ILogger<DeviceService> logger)
        {
            _deviceRepository    = deviceRepository    ?? throw new ArgumentNullException(nameof(deviceRepository));
            _firmwareRepository  = firmwareRepository  ?? throw new ArgumentNullException(nameof(firmwareRepository));
            _jobRepository       = jobRepository       ?? throw new ArgumentNullException(nameof(jobRepository));
            _otaEventRepository  = otaEventRepository  ?? throw new ArgumentNullException(nameof(otaEventRepository));
            _versionService      = versionService      ?? throw new ArgumentNullException(nameof(versionService));
            _auditService        = auditService        ?? throw new ArgumentNullException(nameof(auditService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _mqttService         = mqttService         ?? throw new ArgumentNullException(nameof(mqttService));
            _logger              = logger              ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<DeviceDto> RegisterDeviceAsync(
            RegisterDeviceRequest request,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.MacImeiIp)) throw new ArgumentException("MacImeiIp is required.");
            if (string.IsNullOrWhiteSpace(request.Model)) throw new ArgumentException("Model is required.");
            if (string.IsNullOrWhiteSpace(request.CustomerCode)) throw new ArgumentException("CustomerCode is required.");
            if (string.IsNullOrWhiteSpace(request.ProjectName)) throw new ArgumentException("ProjectName is required.");

            // Use the MAC/IMEI/IP as the unique serial number
            var serialNumber = request.MacImeiIp.Trim().ToUpperInvariant();
            var existing = await _deviceRepository.GetBySerialNumberAsync(serialNumber, cancellationToken);
            if (existing != null)
                throw new InvalidOperationException($"Duplicate identifier: a device with '{request.MacImeiIp}' is already registered.");

            var device = new DeviceEntity
            {
                DeviceId = Guid.NewGuid().ToString(),
                SerialNumber = serialNumber,
                MacImeiIp = request.MacImeiIp.Trim(),
                ProjectName = request.ProjectName.Trim(),
                Model = request.Model.Trim(),
                CustomerName = request.CustomerCode.Trim(),
                CustomerId = request.CustomerCode.Trim(),
                SiteName = request.ProjectName.Trim(),
                CurrentFirmwareVersion = request.CurrentFirmwareVersion ?? "0.0.0",
                Status = DeviceStatus.Active,
                RegisteredAt = DateTime.UtcNow,
                Tags = new List<string>(),
                Metadata = new Dictionary<string, string>(),
                RegisteredByUserId = callerUserId,
                UpdatedAt = DateTime.UtcNow
            };

            await _deviceRepository.InsertAsync(device, cancellationToken);

            _logger.LogInformation("Device '{SerialNumber}' registered by '{Email}'.", device.SerialNumber, callerEmail);

            await _auditService.LogActionAsync(
                AuditAction.DeviceRegistered,
                callerUserId, callerEmail, UserRole.SuperAdmin,
                "Device", device.Id,
                null,
                JsonSerializer.Serialize(new { device.Id, device.SerialNumber, device.Model, device.CustomerId }, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);

            _ = _notificationService.NotifyAsync(
                "Device Registered",
                $"Device {device.Model} ({device.SerialNumber}) was registered.",
                new Dictionary<string, string> { ["type"] = "device_registered", ["deviceId"] = device.DeviceId },
                cancellationToken: CancellationToken.None);

            // Publish registration event to MQTT so the device can receive its platform identifiers
            var mqttPayload = new MqttDeviceRegisteredPayload
            {
                DeviceId               = device.DeviceId,
                SerialNumber           = device.SerialNumber,
                Model                  = device.Model,
                ProjectName            = device.ProjectName,
                CurrentFirmwareVersion = device.CurrentFirmwareVersion,
                RegisteredAt           = device.RegisteredAt.ToString("O")
            };
            var publishTopic = !string.IsNullOrWhiteSpace(request.PublishTopic)
                ? request.PublishTopic.Trim()
                : MqttTopics.DeviceRegisteredPublish(device.SerialNumber);

            device.PublishTopic = publishTopic;
            await _deviceRepository.UpdateAsync(device.Id, device, cancellationToken);

            _ = _mqttService.PublishAsync(
                publishTopic,
                JsonSerializer.Serialize(mqttPayload, _jsonOptions),
                CancellationToken.None);

            return MapToDto(device);
        }

        /// <inheritdoc/>
        public async Task<DeviceDto> UpdateDeviceAsync(
            string deviceId,
            UpdateDeviceRequest request,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(deviceId)) throw new ArgumentException("DeviceId is required.", nameof(deviceId));
            if (request == null) throw new ArgumentNullException(nameof(request));

            var device = await _deviceRepository.GetByIdAsync(deviceId, cancellationToken)
                ?? throw new KeyNotFoundException($"Device '{deviceId}' not found.");

            var oldSnapshot = JsonSerializer.Serialize(new { device.Model, device.CurrentFirmwareVersion, device.PublishTopic }, _jsonOptions);

            if (!string.IsNullOrWhiteSpace(request.Model)) device.Model = request.Model.Trim();
            if (!string.IsNullOrWhiteSpace(request.CurrentFirmwareVersion)) device.CurrentFirmwareVersion = request.CurrentFirmwareVersion.Trim();
            if (!string.IsNullOrWhiteSpace(request.PublishTopic)) device.PublishTopic = request.PublishTopic.Trim();
            device.UpdatedAt = DateTime.UtcNow;

            await _deviceRepository.UpdateAsync(deviceId, device, cancellationToken);

            // Re-publish device info to the configured MQTT topic when one is provided
            if (!string.IsNullOrWhiteSpace(request.PublishTopic))
            {
                var mqttPayload = new MqttDeviceRegisteredPayload
                {
                    DeviceId               = device.DeviceId,
                    SerialNumber           = device.SerialNumber,
                    Model                  = device.Model,
                    ProjectName            = device.ProjectName ?? string.Empty,
                    CurrentFirmwareVersion = device.CurrentFirmwareVersion ?? "0.0.0",
                    RegisteredAt           = device.RegisteredAt.ToString("O")
                };
                _ = _mqttService.PublishAsync(
                    request.PublishTopic.Trim(),
                    JsonSerializer.Serialize(mqttPayload, _jsonOptions),
                    CancellationToken.None);
            }

            await _auditService.LogActionAsync(
                AuditAction.DeviceUpdated,
                callerUserId, callerEmail, UserRole.SuperAdmin,
                "Device", deviceId,
                oldSnapshot,
                JsonSerializer.Serialize(new { device.Model, device.CurrentFirmwareVersion, device.PublishTopic }, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);

            _ = _notificationService.NotifyAsync(
                "Device Updated",
                $"Device {device.SerialNumber} was updated.",
                new Dictionary<string, string> { ["type"] = "device_updated", ["deviceId"] = device.DeviceId },
                cancellationToken: CancellationToken.None);

            return MapToDto(device);
        }

        /// <inheritdoc/>
        public async Task ProcessHeartbeatAsync(string deviceId, DeviceHeartbeatRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(deviceId)) throw new ArgumentException("DeviceId is required.");

            var device = await _deviceRepository.GetByIdAsync(deviceId, cancellationToken);
            if (device == null)
            {
                _logger.LogWarning("Heartbeat received for unknown device '{DeviceId}'.", deviceId);
                return;
            }

            await _deviceRepository.UpdateHeartbeatAsync(device.Id, DateTime.UtcNow, cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.CurrentFirmwareVersion) &&
                request.CurrentFirmwareVersion != device.CurrentFirmwareVersion)
            {
                await _deviceRepository.UpdateFirmwareVersionAsync(device.Id, request.CurrentFirmwareVersion, cancellationToken);
                _logger.LogInformation("Device '{DeviceId}' firmware version updated to '{Version}'.",
                    device.Id, request.CurrentFirmwareVersion);
            }
        }

        /// <inheritdoc/>
        public async Task<CheckUpdateResponse> CheckForUpdateAsync(CheckUpdateRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.DeviceId)) throw new ArgumentException("DeviceId is required.");

            var device = await _deviceRepository.GetByIdAsync(request.DeviceId, cancellationToken)
                ?? throw new KeyNotFoundException($"Device '{request.DeviceId}' not found.");

            if (device.Status != DeviceStatus.Active)
            {
                return new CheckUpdateResponse
                {
                    HasUpdate = false
                };
            }

            var candidates = await _firmwareRepository.GetApprovedForModelAsync(
                device.Model, device.HardwareRevision ?? string.Empty, FirmwareChannel.Production, cancellationToken);

            if (!candidates.Any())
            {
                return new CheckUpdateResponse { HasUpdate = false };
            }

            var current = device.CurrentFirmwareVersion ?? "0.0.0";
            var bestFirmware = candidates
                .Where(f => _versionService.IsCompatible(f, device))
                .OrderByDescending(f => f.Version, StringComparer.Ordinal)
                .FirstOrDefault(f => _versionService.IsNewerVersion(current, f.Version));

            if (bestFirmware == null)
            {
                return new CheckUpdateResponse { HasUpdate = false };
            }

            return new CheckUpdateResponse
            {
                HasUpdate = true,
                FirmwareVersion = bestFirmware.Version,
                JobId = bestFirmware.FirmwareId,
                DownloadUrl = bestFirmware.DownloadUrl,
                Sha256 = bestFirmware.FileSha256,
                FileSizeBytes = bestFirmware.FileSizeBytes,
                ReleaseNotes = bestFirmware.ReleaseNotes,
                IsMandatory = bestFirmware.IsMandate
            };
        }

        /// <inheritdoc/>
        public async Task ReportStatusAsync(ReportStatusRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.JobId)) throw new ArgumentException("JobId is required.");

            var job = await _jobRepository.GetByIdAsync(request.JobId, cancellationToken)
                ?? throw new KeyNotFoundException($"OTA job '{request.JobId}' not found.");

            var device = await _deviceRepository.GetByIdAsync(job.DeviceId, cancellationToken);

            job.Status = request.Status == OtaJobStatus.Succeeded ? OtaJobStatus.Succeeded : OtaJobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            job.ErrorMessage = request.ErrorMessage;
            job.UpdatedAt = DateTime.UtcNow;

            await _jobRepository.UpdateAsync(job.Id, job, cancellationToken);

            _logger.LogInformation("OTA job '{JobId}' status reported: {Status}.", request.JobId, request.Status);

            var isSuccess = job.Status == OtaJobStatus.Succeeded;
            _ = _notificationService.NotifyAsync(
                isSuccess ? "OTA Update Succeeded" : "OTA Update Failed",
                isSuccess
                    ? $"Device {device?.SerialNumber ?? job.DeviceId} updated successfully."
                    : $"OTA update failed for device {device?.SerialNumber ?? job.DeviceId}: {request.ErrorMessage}",
                new Dictionary<string, string>
                {
                    ["type"]    = isSuccess ? "ota_succeeded" : "ota_failed",
                    ["jobId"]   = job.JobId,
                    ["deviceId"] = job.DeviceId
                },
                cancellationToken: CancellationToken.None);
        }

        /// <inheritdoc/>
        public async Task<DeviceDto?> GetDeviceByIdAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(deviceId)) throw new ArgumentException("DeviceId is required.", nameof(deviceId));
            var device = await _deviceRepository.GetByIdAsync(deviceId, cancellationToken);
            return device == null ? null : MapToDto(device);
        }

        /// <inheritdoc/>
        public async Task<PagedResult<DeviceDto>> GetDevicesAsync(string filter, int page, int pageSize, string? projectId = null, List<string>? allowedProjectIds = null, CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var items = await _deviceRepository.SearchAsync(filter, page, pageSize, projectId, allowedProjectIds, cancellationToken);
            var total = await _deviceRepository.CountAsync(filter, projectId, allowedProjectIds, cancellationToken);

            return new PagedResult<DeviceDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        /// <inheritdoc/>
        public async Task SuspendDeviceAsync(
            string deviceId,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            await ChangeDeviceStatusAsync(deviceId, DeviceStatus.Suspended,
                AuditAction.DeviceSuspended, callerUserId, callerEmail, ipAddress, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task DecommissionDeviceAsync(
            string deviceId,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            await ChangeDeviceStatusAsync(deviceId, DeviceStatus.Decommissioned,
                AuditAction.DeviceDecommissioned, callerUserId, callerEmail, ipAddress, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<BulkRegisterResult> BulkRegisterDevicesAsync(
            BulkRegisterDeviceRequest request,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var result = new BulkRegisterResult { Total = request.Devices.Count };

            for (int i = 0; i < request.Devices.Count; i++)
            {
                var deviceRequest = request.Devices[i];
                try
                {
                    await RegisterDeviceAsync(deviceRequest, callerUserId, callerEmail, ipAddress, cancellationToken);
                    result.Succeeded++;
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.Errors.Add(new BulkRegisterError
                    {
                        Row = i + 1,
                        Identifier = deviceRequest.MacImeiIp ?? $"Row {i + 1}",
                        Error = ex.Message
                    });
                    _logger.LogWarning(ex, "Bulk register failed for row {Row}, identifier '{Id}'.", i + 1, deviceRequest.MacImeiIp);
                }
            }

            _ = _notificationService.NotifyAsync(
                "Bulk Device Registration",
                $"{result.Succeeded} device(s) registered, {result.Failed} failed.",
                new Dictionary<string, string> { ["type"] = "bulk_device_registered", ["succeeded"] = result.Succeeded.ToString(), ["failed"] = result.Failed.ToString() },
                cancellationToken: CancellationToken.None);

            return result;
        }

        /// <inheritdoc/>
        public async Task ActivateDeviceAsync(
            string deviceId,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            await ChangeDeviceStatusAsync(deviceId, DeviceStatus.Active,
                AuditAction.DeviceReactivated, callerUserId, callerEmail, ipAddress, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<PagedResult<DeviceOtaHistoryItemDto>> GetDeviceOtaHistoryAsync(
            string deviceId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(deviceId)) throw new ArgumentException("DeviceId is required.", nameof(deviceId));
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            // Fetch rollout jobs and MQTT events in parallel
            var jobsTask   = _jobRepository.GetByDeviceIdAsync(deviceId, cancellationToken);
            var eventsTask = _otaEventRepository.GetByDeviceIdAsync(deviceId, cancellationToken);
            await Task.WhenAll(jobsTask, eventsTask);

            var rolloutItems = jobsTask.Result.Select(j => new DeviceOtaHistoryItemDto
            {
                Id              = j.Id,
                RolloutId       = j.RolloutId,
                RolloutName     = j.RolloutId,
                FirmwareVersion = j.FirmwareVersion ?? string.Empty,
                Status          = j.Status.ToString(),
                Progress        = j.Status.ToString().Equals("Succeeded", StringComparison.OrdinalIgnoreCase) ? 100 : 0,
                Source          = "Rollout",
                CompletedAt     = j.CompletedAt,
                Timestamp       = j.CompletedAt ?? j.CreatedAt,
            });

            var mqttItems = eventsTask.Result.Select(e => new DeviceOtaHistoryItemDto
            {
                Id              = e.Id,
                FirmwareVersion = e.Version ?? string.Empty,
                Status          = e.Status,
                Progress        = e.Progress,
                Source          = "MQTT",
                CompletedAt     = e.Status is "success" or "failed" or "rollback" ? e.Timestamp : null,
                Timestamp       = e.Timestamp,
            });

            var merged = rolloutItems
                .Concat(mqttItems)
                .OrderByDescending(x => x.Timestamp)
                .ToList();

            var total = merged.Count;
            var items = merged
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return PagedResult<DeviceOtaHistoryItemDto>.Create(items, page, pageSize, total);
        }

        /// <inheritdoc/>
        public async Task<List<AvailableFirmwareDto>> GetAvailableFirmwareAsync(
            string deviceId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(deviceId)) throw new ArgumentException("DeviceId is required.", nameof(deviceId));

            var device = await _deviceRepository.GetByIdAsync(deviceId, cancellationToken)
                ?? throw new KeyNotFoundException($"Device '{deviceId}' not found.");

            // Fetch all firmware then apply status + model filter entirely in memory.
            // In-memory status check avoids potential MongoDB enum serialization mismatches.
            var allFirmware = await _firmwareRepository.GetAllAsync(cancellationToken);

            return allFirmware
                .Where(f => f.Status == FirmwareStatus.Approved && f.QaVerifiedAt.HasValue)
                .Where(f =>
                    f.SupportedModels == null ||
                    f.SupportedModels.Count == 0 ||
                    f.SupportedModels.Contains(device.Model, StringComparer.OrdinalIgnoreCase))
                .GroupBy(f => f.Id)
                .Select(g => g.First())
                .OrderByDescending(f => f.Version, StringComparer.Ordinal)
                .Select(f => new AvailableFirmwareDto
                {
                    Id              = f.Id,
                    Version         = f.Version,
                    Channel         = f.Channel.ToString(),
                    Status          = f.Status.ToString(),
                    ReleaseNotes    = f.ReleaseNotes,
                    IsMandate       = f.IsMandate,
                    DownloadUrl     = f.DownloadUrl,
                    FileSizeBytes   = f.FileSizeBytes,
                    ApprovedAt      = f.ApprovedAt?.ToString("o"),
                    SupportedModels = f.SupportedModels ?? new List<string>()
                })
                .ToList();
        }

        /// <inheritdoc/>
        public async Task<string> PushFirmwareToDeviceAsync(
            string deviceId,
            string firmwareVersionId,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(deviceId)) throw new ArgumentException("DeviceId is required.", nameof(deviceId));
            if (string.IsNullOrWhiteSpace(firmwareVersionId)) throw new ArgumentException("FirmwareVersionId is required.", nameof(firmwareVersionId));

            var device = await _deviceRepository.GetByIdAsync(deviceId, cancellationToken)
                ?? throw new KeyNotFoundException($"Device '{deviceId}' not found.");

            if (device.Status != DeviceStatus.Active)
                throw new InvalidOperationException($"Cannot push firmware to a device with status '{device.Status}'. Device must be Active.");

            var firmware = await _firmwareRepository.GetByIdAsync(firmwareVersionId, cancellationToken)
                ?? throw new KeyNotFoundException($"Firmware version '{firmwareVersionId}' not found.");

            var job = new Models.Entities.OtaJobEntity
            {
                JobId              = Guid.NewGuid().ToString(),
                RolloutId          = "direct-push",
                FirmwareId         = firmware.Id,
                FirmwareVersion    = firmware.Version,
                DeviceId           = device.Id,
                DeviceSerialNumber = device.SerialNumber,
                Status             = OtaJobStatus.Pending,
                CreatedAt          = DateTime.UtcNow,
                UpdatedAt          = DateTime.UtcNow,
            };

            await _jobRepository.InsertAsync(job, cancellationToken);

            _logger.LogInformation(
                "Direct firmware push: job '{JobId}' created for device '{DeviceId}' with firmware '{Version}' by '{Email}'.",
                job.JobId, deviceId, firmware.Version, callerEmail);

            await _auditService.LogActionAsync(
                AuditAction.OtaJobRetried,
                callerUserId, callerEmail, UserRole.SuperAdmin,
                "Device", deviceId,
                null,
                JsonSerializer.Serialize(new { FirmwareVersion = firmware.Version, JobId = job.JobId }, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);

            _ = _notificationService.NotifyAsync(
                "Firmware Push Initiated",
                $"Firmware v{firmware.Version} push initiated for device {device.SerialNumber}.",
                new Dictionary<string, string>
                {
                    ["type"]     = "firmware_push",
                    ["deviceId"] = device.DeviceId,
                    ["version"]  = firmware.Version
                },
                cancellationToken: CancellationToken.None);

            return job.Id;
        }

        /// <inheritdoc/>
        public async Task<List<OtaReadyDeviceDto>> GetOtaReadyDevicesAsync(CancellationToken cancellationToken = default)
        {
            var activeDevices = await _deviceRepository.GetByStatusAsync(DeviceStatus.Active, cancellationToken);

            return activeDevices.Select(d => new OtaReadyDeviceDto
            {
                DeviceId               = d.DeviceId,
                SerialNumber           = d.SerialNumber,
                MacImeiIp              = d.MacImeiIp ?? d.SerialNumber,
                Model                  = d.Model,
                ProjectName            = d.ProjectName ?? string.Empty,
                CustomerName           = d.CustomerName,
                CurrentFirmwareVersion = d.CurrentFirmwareVersion ?? "0.0.0",
                PublishTopic           = d.PublishTopic ?? MqttTopics.DeviceRegisteredPublish(d.SerialNumber),
                Status                 = d.Status.ToString(),
                LastHeartbeatAt        = d.LastHeartbeatAt,
                RegisteredAt           = d.RegisteredAt,
                OtaStatus              = d.OtaStatus ?? "none",
                OtaProgress            = d.OtaProgress,
                OtaTargetVersion       = d.OtaTargetVersion ?? string.Empty,
                OtaUpdatedAt           = d.OtaUpdatedAt,
            }).ToList();
        }

        // ── Private helpers ─────────────────────────────────────────────────────────

        private async Task ChangeDeviceStatusAsync(
            string deviceId,
            DeviceStatus newStatus,
            AuditAction action,
            string callerUserId,
            string callerEmail,
            string ipAddress,
            CancellationToken cancellationToken)
        {
            var device = await _deviceRepository.GetByIdAsync(deviceId, cancellationToken)
                ?? throw new KeyNotFoundException($"Device '{deviceId}' not found.");

            var oldStatus = device.Status;
            device.Status = newStatus;
            device.UpdatedAt = DateTime.UtcNow;
            await _deviceRepository.UpdateAsync(deviceId, device, cancellationToken);

            _logger.LogInformation("Device '{DeviceId}' status changed from '{Old}' to '{New}' by '{Email}'.",
                deviceId, oldStatus, newStatus, callerEmail);

            await _auditService.LogActionAsync(
                action,
                callerUserId, callerEmail, UserRole.SuperAdmin,
                "Device", deviceId,
                JsonSerializer.Serialize(new { Status = oldStatus.ToString() }, _jsonOptions),
                JsonSerializer.Serialize(new { Status = newStatus.ToString() }, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);

            _ = _notificationService.NotifyAsync(
                $"Device {newStatus}",
                $"Device {device.SerialNumber} status changed to {newStatus}.",
                new Dictionary<string, string> { ["type"] = $"device_{newStatus.ToString().ToLower()}", ["deviceId"] = device.DeviceId },
                cancellationToken: CancellationToken.None);
        }

        private static DeviceDto MapToDto(DeviceEntity d) => new DeviceDto
        {
            Id = d.Id,
            DeviceId = d.DeviceId,
            MacImeiIp = d.MacImeiIp ?? d.SerialNumber,
            ProjectName = d.ProjectName ?? d.SiteName,
            Model = d.Model,
            CustomerId = d.CustomerId,
            CustomerName = d.CustomerName,
            CurrentFirmwareVersion = d.CurrentFirmwareVersion,
            PreviousFirmwareVersion = d.PreviousFirmwareVersion,
            Status = d.Status.ToString(),
            LastHeartbeatAt = d.LastHeartbeatAt,
            RegisteredAt = d.RegisteredAt,
            UpdatedAt = d.UpdatedAt,
            RegisteredByUserId = d.RegisteredByUserId,
            PublishTopic = d.PublishTopic,
            Tags = d.Tags,
            Metadata = d.Metadata,
            OtaStatus = d.OtaStatus,
            OtaProgress = d.OtaProgress,
            OtaTargetVersion = d.OtaTargetVersion,
            OtaUpdatedAt = d.OtaUpdatedAt,
        };
    }
}
