using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MQTTnet.Client;
using OTA.API.Models.DTOs;
using OTA.API.Models.Entities;
using OTA.API.Models.Enums;
using OTA.API.Models.Settings;
using OTA.API.Repositories.Interfaces;
using OTA.API.Services;
using OTA.API.Services.Interfaces;

namespace OTA.API.BackgroundJobs
{
    /// <summary>
    /// Long-running hosted service that maintains the MQTT broker connection and processes
    /// messages from IoT devices according to the OTA FSI specification.
    ///
    /// Subscriptions (Device → Server):
    ///   • OTA/Card2Web/Server     — device OTA request
    ///   • OTA/+/Status            — device OTA status updates
    ///
    /// Publishes (Server → Device):
    ///   • OTA/{deviceId}/Card     — OTA firmware metadata
    /// </summary>
    public sealed class MqttBackgroundService : BackgroundService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy         = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive  = true,
            DefaultIgnoreCondition       = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private readonly MqttService _mqttService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly MqttSettings _settings;
        private readonly ILogger<MqttBackgroundService> _logger;

        public MqttBackgroundService(
            MqttService mqttService,
            IServiceScopeFactory scopeFactory,
            IOptions<MqttSettings> settings,
            ILogger<MqttBackgroundService> logger)
        {
            _mqttService  = mqttService  ?? throw new ArgumentNullException(nameof(mqttService));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _settings     = settings.Value;
            _logger       = logger       ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Register message handler before connecting.
            _mqttService.MessageReceived += OnMessageReceivedAsync;

            // Attempt initial connection with retry.
            await ConnectWithRetryAsync(stoppingToken);

            // Keep the service alive; reconnect on disconnect.
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!_mqttService.IsConnected)
                    {
                        _logger.LogWarning("MQTT: Connection lost — attempting reconnect in {Delay}s.",
                            _settings.ReconnectDelaySecs);
                        await Task.Delay(TimeSpan.FromSeconds(_settings.ReconnectDelaySecs), stoppingToken);
                        await ConnectWithRetryAsync(stoppingToken);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MQTT: Unexpected error in reconnect loop.");
                }
            }

            _mqttService.MessageReceived -= OnMessageReceivedAsync;
            await _mqttService.StopAsync(CancellationToken.None);
            _logger.LogInformation("MQTT: Background service stopped.");
        }

        // ── Connection ────────────────────────────────────────────────────────

        private async Task ConnectWithRetryAsync(CancellationToken cancellationToken)
        {
            const int maxAttempts = 5;
            int attempt = 0;

            while (!cancellationToken.IsCancellationRequested && attempt < maxAttempts)
            {
                attempt++;
                try
                {
                    await _mqttService.StartAsync(cancellationToken);

                    // Subscribe to all device topics.
                    await _mqttService.SubscribeAsync(MqttTopics.OtaRequestSubscribe, cancellationToken);
                    await _mqttService.SubscribeAsync(MqttTopics.OtaStatusSubscribeFilter, cancellationToken);

                    _logger.LogInformation("MQTT: Ready. Listening on '{Request}' and '{Status}'.",
                        MqttTopics.OtaRequestSubscribe, MqttTopics.OtaStatusSubscribeFilter);
                    return;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    if (_settings.OptionalInDevelopment)
                    {
                        _logger.LogWarning(
                            "MQTT: Could not connect to broker {Host}:{Port} (attempt {A}/{Max}). " +
                            "Running without MQTT (OptionalInDevelopment=true). Error: {Msg}",
                            _settings.BrokerHost, _settings.BrokerPort, attempt, maxAttempts, ex.Message);
                        return;
                    }

                    _logger.LogError(ex,
                        "MQTT: Could not connect to broker {Host}:{Port} (attempt {A}/{Max}). " +
                        "Retrying in {Delay}s.",
                        _settings.BrokerHost, _settings.BrokerPort, attempt, maxAttempts,
                        _settings.ReconnectDelaySecs);

                    if (attempt < maxAttempts)
                        await Task.Delay(TimeSpan.FromSeconds(_settings.ReconnectDelaySecs), cancellationToken);
                }
            }
        }

        // ── Message Routing ────────────────────────────────────────────────────

        private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
        {
            var topic   = args.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);

            _logger.LogInformation("MQTT: Received message on '{Topic}'. PayloadLength={Len}.", topic, payload.Length);

            try
            {
                if (topic == MqttTopics.OtaRequestSubscribe)
                {
                    // Some devices publish status updates to the shared request topic instead of
                    // OTA/{deviceId}/Status.  Peek at the envelope key to decide which handler to use.
                    var statusDeviceId = TryExtractStatusDeviceId(payload);
                    if (statusDeviceId is not null)
                    {
                        _logger.LogInformation(
                            "MQTT: Status payload detected on request topic from device '{DeviceId}'. Routing to status handler.",
                            statusDeviceId);
                        await HandleOtaStatusAsync(statusDeviceId, payload, CancellationToken.None);
                    }
                    else
                    {
                        await HandleOtaRequestAsync(payload, CancellationToken.None);
                    }
                }
                else
                {
                    var deviceId = MqttTopics.ExtractDeviceIdFromStatusTopic(topic);
                    if (deviceId is not null)
                        await HandleOtaStatusAsync(deviceId, payload, CancellationToken.None);
                    else
                        _logger.LogWarning("MQTT: Unhandled topic '{Topic}'.", topic);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MQTT: Error processing message on topic '{Topic}'.", topic);
            }
        }

        // ── Payload type detection ────────────────────────────────────────────

        /// <summary>
        /// Returns the deviceId when the payload is an OTA status envelope (contains "otaStatus"),
        /// or null when it is an OTA request envelope.
        /// </summary>
        private static string? TryExtractStatusDeviceId(string payload)
        {
            try
            {
                var envelope = JsonSerializer.Deserialize<MqttOtaStatusEnvelope>(payload, JsonOptions);
                var id = envelope?.OtaStatus?.DeviceId;
                return string.IsNullOrWhiteSpace(id) ? null : id;
            }
            catch
            {
                return null;
            }
        }

        // ── OTA Request Handler  (FSI §5 Step 1) ──────────────────────────────

        private async Task HandleOtaRequestAsync(string rawPayload, CancellationToken cancellationToken)
        {
            MqttOtaRequestEnvelope? envelope;
            try
            {
                envelope = JsonSerializer.Deserialize<MqttOtaRequestEnvelope>(rawPayload, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "MQTT: OTA request payload is not valid JSON.");
                return;
            }

            var request = envelope?.OtaRequest;
            if (request is null || string.IsNullOrWhiteSpace(request.DeviceId))
            {
                _logger.LogWarning("MQTT: OTA request missing required 'deviceId' field.");
                return;
            }

            _logger.LogInformation(
                "MQTT: OTA request from device '{DeviceId}', version '{Version}', trigger='{Trigger}'.",
                request.DeviceId, request.CurrentVersion, request.Trigger);

            using var scope = _scopeFactory.CreateScope();
            var deviceRepo = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();

            // Device identifier in MQTT maps to SerialNumber (MAC/IMEI/IP stored as upper-case serial).
            var serialNumber = request.DeviceId.Trim().ToUpperInvariant();
            var device = await deviceRepo.GetBySerialNumberAsync(serialNumber, cancellationToken);

            if (device is null)
            {
                _logger.LogWarning("MQTT: OTA request from unknown device '{DeviceId}'. Ignored.", request.DeviceId);
                return;
            }

            if (device.Status != DeviceStatus.Active)
            {
                _logger.LogInformation(
                    "MQTT: Device '{DeviceId}' is not Active (status={Status}). No OTA sent.",
                    request.DeviceId, device.Status);

                var noUpdatePayload = JsonSerializer.Serialize(new MqttNoUpdateEnvelope
                {
                    OtaUpdate = new MqttNoUpdate
                    {
                        DeviceId       = request.DeviceId,
                        Status         = "device-not-active",
                        CurrentVersion = request.CurrentVersion
                    }
                }, JsonOptions);

                await _mqttService.PublishOtaMetadataAsync(request.DeviceId, noUpdatePayload, cancellationToken);
                return;
            }

            // Update heartbeat.
            await deviceRepo.UpdateHeartbeatAsync(device.Id, DateTime.UtcNow, cancellationToken);
            if (!string.IsNullOrWhiteSpace(request.CurrentVersion) &&
                request.CurrentVersion != device.CurrentFirmwareVersion)
            {
                await deviceRepo.UpdateFirmwareVersionAsync(device.Id, request.CurrentVersion, cancellationToken);
            }

            var firmwareRepo = scope.ServiceProvider.GetRequiredService<IFirmwareRepository>();

            var currentVersion = string.IsNullOrWhiteSpace(request.CurrentVersion)
                ? (device.CurrentFirmwareVersion ?? "0.0.0")
                : request.CurrentVersion;

            FirmwareVersionEntity? best = null;

            // ── 1. Honor a queued direct-push job first (Release Manager intent) ──
            // If the Release Manager pushed a specific firmware via the Device OTA screen,
            // there will be an OTA job in Queued (or InProgress) state for this device.
            // That explicit choice MUST win over model-based discovery; otherwise we'd
            // ship the latest "approved" firmware instead of the one the user selected.
            var jobRepo = scope.ServiceProvider.GetRequiredService<IOtaJobRepository>();
            var deviceJobs = await jobRepo.GetByDeviceIdAsync(device.Id, cancellationToken);
            var pushedJob = deviceJobs
                .Where(j => j.Status == OtaJobStatus.Queued || j.Status == OtaJobStatus.InProgress)
                .OrderByDescending(j => j.CreatedAt)
                .FirstOrDefault();

            if (pushedJob != null)
            {
                var jobFirmware = await firmwareRepo.GetByIdAsync(pushedJob.FirmwareId, cancellationToken);
                if (jobFirmware != null && !string.IsNullOrWhiteSpace(jobFirmware.DownloadUrl))
                {
                    best = jobFirmware;
                    _logger.LogInformation(
                        "MQTT: Serving Release-Manager-pushed firmware v{Version} (job '{JobId}') to device '{DeviceId}'.",
                        jobFirmware.Version, pushedJob.JobId, request.DeviceId);
                }
            }

            // ── 2. Fall back to model-based discovery if no job was pushed ──
            if (best is null)
            {
                var candidates = await firmwareRepo.GetApprovedForModelAsync(
                    device.Model,
                    device.HardwareRevision,
                    null,   // match any channel — firmware channel assignment is not used to restrict OTA delivery
                    cancellationToken);

                var versionSvc = scope.ServiceProvider.GetRequiredService<IVersionComparisonService>();

                best = candidates
                    .Where(f => versionSvc.IsCompatible(f, device))
                    .OrderByDescending(f => f.Version, StringComparer.Ordinal)
                    .FirstOrDefault(f => versionSvc.IsNewerVersion(currentVersion, f.Version));

                // Skip firmware that has no download URL — device can't fetch it.
                if (best is not null && string.IsNullOrWhiteSpace(best.DownloadUrl))
                {
                    _logger.LogWarning(
                        "MQTT: Best firmware v{Version} for device '{DeviceId}' has no download URL — skipping OTA dispatch.",
                        best.Version, request.DeviceId);
                    best = null;
                }
            }

            if (best is null)
            {
                _logger.LogInformation(
                    "MQTT: Device '{DeviceId}' is already up to date (version='{Version}').",
                    request.DeviceId, currentVersion);

                var noUpdatePayload = JsonSerializer.Serialize(new MqttNoUpdateEnvelope
                {
                    OtaUpdate = new MqttNoUpdate
                    {
                        DeviceId       = request.DeviceId,
                        Status         = "up-to-date",
                        CurrentVersion = currentVersion
                    }
                }, JsonOptions);

                await _mqttService.PublishOtaMetadataAsync(request.DeviceId, noUpdatePayload, cancellationToken);
                return;
            }

            // Publish OTA metadata to the device (FSI §6).
            var updateEnvelope = new MqttOtaUpdateEnvelope
            {
                OtaUpdate = new MqttOtaUpdate
                {
                    Description       = best.ReleaseNotes ?? string.Empty,
                    Version           = best.Version,
                    DeviceId          = request.DeviceId,
                    ReleaseDate       = best.ApprovedAt?.ToString("o") ?? best.CreatedAt.ToString("o"),
                    Mandatory         = best.IsMandate,
                    RollbackSupported = true,
                    Files             = new List<MqttFirmwareFile>
                    {
                        new MqttFirmwareFile
                        {
                            FileIndex   = 1,
                            DownloadUrl = best.DownloadUrl,
                            FileSize    = best.FileSizeBytes,
                            Checksum    = new MqttChecksum
                            {
                                Type  = "SHA256",
                                Value = best.FileSha256
                            }
                        }
                    }
                }
            };

            var updatePayload = JsonSerializer.Serialize(updateEnvelope, JsonOptions);
            await _mqttService.PublishOtaMetadataAsync(request.DeviceId, updatePayload, cancellationToken);

            _logger.LogInformation(
                "MQTT: OTA metadata published to device '{DeviceId}'. Version='{Version}', Mandatory={Mandatory}.",
                request.DeviceId, best.Version, best.IsMandate);
        }

        // ── OTA Status Handler  (FSI §8, §11–13, §16) ─────────────────────────

        private async Task HandleOtaStatusAsync(
            string deviceId,
            string rawPayload,
            CancellationToken cancellationToken)
        {
            MqttOtaStatusEnvelope? envelope;
            try
            {
                envelope = JsonSerializer.Deserialize<MqttOtaStatusEnvelope>(rawPayload, JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "MQTT: OTA status payload from '{DeviceId}' is not valid JSON.", deviceId);
                return;
            }

            var status = envelope?.OtaStatus;
            if (status is null)
            {
                _logger.LogWarning("MQTT: OTA status message from '{DeviceId}' missing 'otaStatus' field.", deviceId);
                return;
            }

            // Normalise device-reported status to the platform's canonical vocabulary.
            // Devices may emit variants like "started", "in-progress", "rolled-back" — map them
            // to the strings used everywhere else in the system.
            status.Status = NormaliseOtaStatus(status.Status);

            _logger.LogInformation(
                "MQTT: Status update from '{DeviceId}' — Status='{Status}', Progress={Progress}%, " +
                "Version='{Version}', ErrorCode={ErrorCode}.",
                deviceId, status.Status, status.Progress, status.CurrentVersion, status.ErrorCode);

            using var scope = _scopeFactory.CreateScope();
            var deviceRepo = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();

            var serialNumber = deviceId.Trim().ToUpperInvariant();
            var device = await deviceRepo.GetBySerialNumberAsync(serialNumber, cancellationToken);

            if (device is null)
            {
                _logger.LogWarning("MQTT: Status update from unknown device '{DeviceId}'. Ignored.", deviceId);
                return;
            }

            // Always sync heartbeat on any status packet.
            await deviceRepo.UpdateHeartbeatAsync(device.Id, DateTime.UtcNow, cancellationToken);

            // Persist live OTA progress for every packet so the dashboard stays current —
            // BUT only when the reported version matches the firmware we actually pushed
            // (or no push is active). Stale messages from a prior OTA must not overwrite
            // the progress fields and confuse the dashboard.
            var stalePacket =
                !string.IsNullOrWhiteSpace(device.PendingFirmwareVersion) &&
                !string.IsNullOrWhiteSpace(status.CurrentVersion) &&
                !string.Equals(device.PendingFirmwareVersion.Trim(),
                               status.CurrentVersion.Trim(),
                               StringComparison.OrdinalIgnoreCase);

            if (!stalePacket)
            {
                await deviceRepo.UpdateOtaProgressAsync(
                    device.Id,
                    status.Status,
                    status.Progress,
                    status.CurrentVersion,
                    cancellationToken);
            }
            else
            {
                _logger.LogDebug(
                    "MQTT: Skipping OTA progress write for stale packet from '{DeviceId}' (reported v{Reported}, pending v{Pending}).",
                    deviceId, status.CurrentVersion, device.PendingFirmwareVersion);
            }

            // Publish device data to the device's configured publish topic on every OTA status update.
            var publishTopic = !string.IsNullOrWhiteSpace(device.PublishTopic)
                ? device.PublishTopic
                : MqttTopics.DeviceRegisteredPublish(device.SerialNumber);

            var otaPayload = new MqttOtaProcessPayload
            {
                DeviceId               = device.DeviceId,
                SerialNumber           = device.SerialNumber,
                Model                  = device.Model,
                ProjectName            = device.ProjectName ?? string.Empty,
                OtaStatus              = status.Status,
                OtaProgress            = status.Progress,
                CurrentFirmwareVersion = status.CurrentVersion ?? device.CurrentFirmwareVersion ?? "0.0.0",
                OtaTargetVersion       = status.CurrentVersion,
                ErrorCode              = status.ErrorCode,
                Timestamp              = DateTime.UtcNow.ToString("O"),
            };

            _ = _mqttService.PublishAsync(
                publishTopic,
                JsonSerializer.Serialize(otaPayload, JsonOptions),
                cancellationToken);

            // Log a history event for start / success / failed / rollback.
            // "inprogress" packets are intentionally skipped to avoid flooding the history.
            var logStatuses = new[] { "start", "success", "failed", "rollback" };
            if (logStatuses.Contains(status.Status, StringComparer.OrdinalIgnoreCase))
            {
                var otaEventRepo = scope.ServiceProvider.GetRequiredService<IDeviceOtaEventRepository>();
                await otaEventRepo.LogAsync(new OTA.API.Models.Entities.DeviceOtaEventEntity
                {
                    DeviceId     = device.Id,
                    SerialNumber = device.SerialNumber,
                    Status       = status.Status.ToLowerInvariant(),
                    Progress     = status.Progress,
                    Version      = status.CurrentVersion,
                    ErrorCode    = status.ErrorCode,
                    Timestamp    = DateTime.UtcNow,
                }, cancellationToken);
            }

            // On start — log only. We deliberately DO NOT call UpdateFirmwareVersionAsync
            // here: some device firmwares report the *target* version on start/inprogress
            // packets instead of the currently-installed version, which would prematurely
            // flip the FIRMWARE column to v1.1.21 while the device is still installing.
            // The currently-installed version is only authoritative on a "success" packet.
            if (string.Equals(status.Status, "start", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "MQTT: Device '{DeviceId}' started OTA (reported version '{Version}').",
                    deviceId, status.CurrentVersion);
            }

            // A terminal status report only invalidates the active job when the device is
            // reporting the version we actually pushed. Stale messages (e.g. retained MQTT
            // success for a previous firmware) must NOT wipe HasActiveOtaJob — otherwise a
            // device that has just been queued for v1.1.21 falls out of the Pending tab the
            // moment a leftover "success: v1.1.20" arrives.
            //
            // Match rules:
            //   - If no PendingFirmwareVersion is stored → no active push → behave as before.
            //   - If PendingFirmwareVersion matches reported CurrentVersion → real terminal
            //     for the active push → clear the flag.
            //   - Otherwise → stale message, leave the flag alone.
            bool MatchesActivePush(string? reportedVersion)
            {
                if (string.IsNullOrWhiteSpace(device.PendingFirmwareVersion)) return true;
                if (string.IsNullOrWhiteSpace(reportedVersion)) return false;
                return string.Equals(
                    device.PendingFirmwareVersion.Trim(),
                    reportedVersion.Trim(),
                    StringComparison.OrdinalIgnoreCase);
            }

            // Find the active job (Created/Queued/InProgress) for this device whose
            // FirmwareVersion matches the reported version, and flip it to the supplied
            // terminal status. Without this, jobs sit at Queued forever and the OTA
            // History report shows stale rows even after the device finished.
            var jobRepo = scope.ServiceProvider.GetRequiredService<IOtaJobRepository>();
            async Task<bool> CompleteActiveJobAsync(OtaJobStatus terminalStatus, string? reportedVersion)
            {
                var allJobs = await jobRepo.GetByDeviceIdAsync(device.Id, cancellationToken);
                var active = allJobs
                    .Where(j => j.Status == OtaJobStatus.Created
                             || j.Status == OtaJobStatus.Queued
                             || j.Status == OtaJobStatus.InProgress)
                    .Where(j => string.IsNullOrWhiteSpace(reportedVersion)
                             || string.Equals(j.FirmwareVersion?.Trim(), reportedVersion.Trim(), StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(j => j.CreatedAt)
                    .FirstOrDefault();

                if (active is null) return false;

                active.Status      = terminalStatus;
                active.CompletedAt = DateTime.UtcNow;
                active.UpdatedAt   = DateTime.UtcNow;
                if (terminalStatus == OtaJobStatus.Failed && string.IsNullOrWhiteSpace(active.FailureReason))
                    active.FailureReason = $"ErrorCode={status.ErrorCode}";

                await jobRepo.UpdateAsync(active.Id, active, cancellationToken);
                return true;
            }

            // On success — update to the newly installed version. We deliberately do NOT
            // clear OtaStatus/OtaProgress: keeping them at "success / 100%" lets the OTA
            // Progress cell display "Updated v{version} 100%" until the next push begins
            // (PushFirmwareToDeviceAsync calls ClearOtaProgressAsync at that point). What
            // we DO clear is the bookkeeping flag (HasActiveOtaJob + PendingFirmwareVersion)
            // so the device leaves the Pending tab.
            if (string.Equals(status.Status, "success", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(status.CurrentVersion))
            {
                await deviceRepo.UpdateFirmwareVersionAsync(device.Id, status.CurrentVersion, cancellationToken);

                if (MatchesActivePush(status.CurrentVersion))
                {
                    await deviceRepo.SetActiveOtaJobAsync(device.Id, false, null, cancellationToken);
                    var updated = await CompleteActiveJobAsync(OtaJobStatus.Succeeded, status.CurrentVersion);
                    _logger.LogInformation(
                        "MQTT: Device '{DeviceId}' successfully updated to firmware '{Version}'. JobUpdated={JobUpdated}.",
                        deviceId, status.CurrentVersion, updated);
                }
                else
                {
                    _logger.LogInformation(
                        "MQTT: Device '{DeviceId}' reported success for v{Version} but a push for v{Pending} is still active. Treating as stale, keeping Pending state.",
                        deviceId, status.CurrentVersion, device.PendingFirmwareVersion);
                }
            }

            // On failed — clear active flag only when this failure relates to the active push.
            if (string.Equals(status.Status, "failed", StringComparison.OrdinalIgnoreCase))
            {
                if (MatchesActivePush(status.CurrentVersion))
                {
                    await deviceRepo.ClearOtaProgressAsync(device.Id, cancellationToken);
                    await deviceRepo.SetActiveOtaJobAsync(device.Id, false, null, cancellationToken);
                    var updated = await CompleteActiveJobAsync(OtaJobStatus.Failed, status.CurrentVersion);
                    _logger.LogWarning(
                        "MQTT: Device '{DeviceId}' OTA failed. ErrorCode={ErrorCode}. JobUpdated={JobUpdated}.",
                        deviceId, status.ErrorCode, updated);
                }
                else
                {
                    _logger.LogWarning(
                        "MQTT: Device '{DeviceId}' reported failed for v{Version} but a push for v{Pending} is still active. Stale message ignored.",
                        deviceId, status.CurrentVersion, device.PendingFirmwareVersion);
                }
            }

            // On rollback — same version-match guard.
            if (string.Equals(status.Status, "rollback", StringComparison.OrdinalIgnoreCase))
            {
                if (MatchesActivePush(status.CurrentVersion))
                {
                    await deviceRepo.ClearOtaProgressAsync(device.Id, cancellationToken);
                    await deviceRepo.SetActiveOtaJobAsync(device.Id, false, null, cancellationToken);
                    var updated = await CompleteActiveJobAsync(OtaJobStatus.Cancelled, status.CurrentVersion);
                    _logger.LogWarning(
                        "MQTT: Device '{DeviceId}' rolled back to '{Version}'. ErrorCode={ErrorCode}. JobUpdated={JobUpdated}.",
                        deviceId, status.CurrentVersion, status.ErrorCode, updated);
                }
                else
                {
                    _logger.LogWarning(
                        "MQTT: Device '{DeviceId}' reported rollback for v{Version} but a push for v{Pending} is still active. Stale message ignored.",
                        deviceId, status.CurrentVersion, device.PendingFirmwareVersion);
                }
            }
        }

        /// <summary>
        /// Maps device-reported OTA status strings to the platform's canonical vocabulary
        /// (start | inprogress | success | failed | rollback). Unknown values are returned
        /// as-is in lowercase.
        /// </summary>
        private static string NormaliseOtaStatus(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            var s = raw.Trim().ToLowerInvariant();
            return s switch
            {
                "start" or "started" or "starting"           => "start",
                "inprogress" or "in-progress" or "in_progress"
                    or "downloading" or "installing"
                    or "running"                              => "inprogress",
                "success" or "succeeded" or "completed"
                    or "complete" or "ok"                     => "success",
                "failed" or "failure" or "error"              => "failed",
                "rollback" or "rolled-back" or "rolled_back"
                    or "rolledback" or "reverted"             => "rollback",
                _ => s,
            };
        }
    }
}
