using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MQTTnet.Client;
using OTA.API.Models.DTOs;
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

            // Check whether an update is available via the firmware repository.
            var firmwareRepo = scope.ServiceProvider.GetRequiredService<IFirmwareRepository>();

            var candidates = await firmwareRepo.GetApprovedForModelAsync(
                device.Model,
                device.HardwareRevision,
                null,   // match any channel — firmware channel assignment is not used to restrict OTA delivery
                cancellationToken);

            var versionSvc = scope.ServiceProvider.GetRequiredService<IVersionComparisonService>();
            var currentVersion = string.IsNullOrWhiteSpace(request.CurrentVersion)
                ? (device.CurrentFirmwareVersion ?? "0.0.0")
                : request.CurrentVersion;

            var best = candidates
                .Where(f => versionSvc.IsCompatible(f, device))
                .OrderByDescending(f => f.Version, StringComparer.Ordinal)
                .FirstOrDefault(f => versionSvc.IsNewerVersion(currentVersion, f.Version));

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

            // Persist live OTA progress for every packet so the dashboard stays current.
            await deviceRepo.UpdateOtaProgressAsync(
                device.Id,
                status.Status,
                status.Progress,
                status.CurrentVersion,
                cancellationToken);

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

            // On start — record the version the device is currently running before the update.
            if (string.Equals(status.Status, "start", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(status.CurrentVersion))
            {
                await deviceRepo.UpdateFirmwareVersionAsync(device.Id, status.CurrentVersion, cancellationToken);
                _logger.LogInformation(
                    "MQTT: Device '{DeviceId}' started OTA. Current version recorded as '{Version}'.",
                    deviceId, status.CurrentVersion);
            }

            // On success — update to the newly installed version.
            if (string.Equals(status.Status, "success", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(status.CurrentVersion))
            {
                await deviceRepo.UpdateFirmwareVersionAsync(device.Id, status.CurrentVersion, cancellationToken);
                _logger.LogInformation(
                    "MQTT: Device '{DeviceId}' successfully updated to firmware '{Version}'.",
                    deviceId, status.CurrentVersion);
            }

            // On rollback — log but keep the previous firmware version (already stored).
            if (string.Equals(status.Status, "rollback", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "MQTT: Device '{DeviceId}' rolled back to '{Version}'. ErrorCode={ErrorCode}.",
                    deviceId, status.CurrentVersion, status.ErrorCode);
            }
        }
    }
}
