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
        private readonly IVersionComparisonService _versionService;
        private readonly IAuditService _auditService;
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
            IVersionComparisonService versionService,
            IAuditService auditService,
            ILogger<DeviceService> logger)
        {
            _deviceRepository = deviceRepository ?? throw new ArgumentNullException(nameof(deviceRepository));
            _firmwareRepository = firmwareRepository ?? throw new ArgumentNullException(nameof(firmwareRepository));
            _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
            _versionService = versionService ?? throw new ArgumentNullException(nameof(versionService));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            if (string.IsNullOrWhiteSpace(request.SerialNumber)) throw new ArgumentException("SerialNumber is required.");
            if (string.IsNullOrWhiteSpace(request.Model)) throw new ArgumentException("Model is required.");
            if (string.IsNullOrWhiteSpace(request.HardwareRevision)) throw new ArgumentException("HardwareRevision is required.");

            var existing = await _deviceRepository.GetBySerialNumberAsync(request.SerialNumber.Trim().ToUpperInvariant(), cancellationToken);
            if (existing != null)
                throw new InvalidOperationException($"A device with serial number '{request.SerialNumber}' is already registered.");

            var device = new DeviceEntity
            {
                DeviceId = Guid.NewGuid().ToString(),
                SerialNumber = request.SerialNumber.Trim().ToUpperInvariant(),
                Model = request.Model.Trim(),
                HardwareRevision = request.HardwareRevision?.Trim(),
                CustomerId = request.CustomerId,
                CustomerName = request.CustomerName,
                SiteId = request.SiteId,
                SiteName = request.SiteName,
                CurrentFirmwareVersion = request.CurrentFirmwareVersion ?? "0.0.0",
                Status = DeviceStatus.Active,
                RegisteredAt = DateTime.UtcNow,
                Tags = request.Tags ?? new List<string>(),
                Metadata = request.Metadata ?? new Dictionary<string, string>(),
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

            var oldSnapshot = JsonSerializer.Serialize(new { device.SiteId, device.Tags }, _jsonOptions);

            if (request.SiteId != null) device.SiteId = request.SiteId;
            if (request.SiteName != null) device.SiteName = request.SiteName;
            if (request.Tags != null) device.Tags = request.Tags;
            if (request.Metadata != null) device.Metadata = request.Metadata;
            if (request.Status.HasValue) device.Status = request.Status.Value;
            device.UpdatedAt = DateTime.UtcNow;

            await _deviceRepository.UpdateAsync(deviceId, device, cancellationToken);

            await _auditService.LogActionAsync(
                AuditAction.DeviceUpdated,
                callerUserId, callerEmail, UserRole.SuperAdmin,
                "Device", deviceId,
                oldSnapshot,
                JsonSerializer.Serialize(new { device.SiteId, device.Status }, _jsonOptions),
                ipAddress,
                cancellationToken: cancellationToken);

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
        }

        /// <inheritdoc/>
        public async Task<DeviceDto?> GetDeviceByIdAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(deviceId)) throw new ArgumentException("DeviceId is required.", nameof(deviceId));
            var device = await _deviceRepository.GetByIdAsync(deviceId, cancellationToken);
            return device == null ? null : MapToDto(device);
        }

        /// <inheritdoc/>
        public async Task<PagedResult<DeviceDto>> GetDevicesAsync(string filter, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var items = await _deviceRepository.SearchAsync(filter, page, pageSize, cancellationToken);
            var total = await _deviceRepository.CountAsync(filter, cancellationToken);

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
        }

        private static DeviceDto MapToDto(DeviceEntity d) => new DeviceDto
        {
            DeviceId = d.DeviceId,
            SerialNumber = d.SerialNumber,
            Model = d.Model,
            HardwareRevision = d.HardwareRevision,
            CustomerId = d.CustomerId,
            CustomerName = d.CustomerName,
            SiteId = d.SiteId,
            SiteName = d.SiteName,
            CurrentFirmwareVersion = d.CurrentFirmwareVersion,
            PreviousFirmwareVersion = d.PreviousFirmwareVersion,
            Status = d.Status.ToString(),
            LastHeartbeatAt = d.LastHeartbeatAt,
            RegisteredAt = d.RegisteredAt,
            UpdatedAt = d.UpdatedAt,
            RegisteredByUserId = d.RegisteredByUserId,
            Tags = d.Tags,
            Metadata = d.Metadata
        };
    }
}
