using System.Text;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using OTA.API.Models.DTOs;
using OTA.API.Models.Settings;
using OTA.API.Services.Interfaces;

namespace OTA.API.Services
{
    /// <summary>
    /// Manages the MQTT client connection and exposes publish operations.
    /// Registered as a singleton so the connection is shared across the application.
    /// The <see cref="MqttBackgroundService"/> is responsible for calling
    /// <see cref="StartAsync"/> / <see cref="StopAsync"/> during host lifetime.
    /// </summary>
    public sealed class MqttService : IMqttService, IAsyncDisposable
    {
        private readonly MqttSettings _settings;
        private readonly ILogger<MqttService> _logger;
        private readonly IMqttClient _mqttClient;

        // Subscribers can hook into incoming messages (used by MqttBackgroundService).
        public event Func<MqttApplicationMessageReceivedEventArgs, Task>? MessageReceived;

        public bool IsConnected => _mqttClient.IsConnected;

        public MqttService(IOptions<MqttSettings> settings, ILogger<MqttService> logger)
        {
            _settings    = settings.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger      = logger ?? throw new ArgumentNullException(nameof(logger));
            _mqttClient  = new MqttFactory().CreateMqttClient();

            _mqttClient.ApplicationMessageReceivedAsync += args =>
            {
                return MessageReceived?.Invoke(args) ?? Task.CompletedTask;
            };

            _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
        }

        /// <summary>Connects to the configured MQTT broker.</summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var options = BuildOptions();
            _logger.LogInformation("MQTT: Connecting to broker {Host}:{Port} as '{ClientId}'…",
                _settings.BrokerHost, _settings.BrokerPort, _settings.ClientId);

            await _mqttClient.ConnectAsync(options, cancellationToken);
            _logger.LogInformation("MQTT: Connected to broker {Host}:{Port}.",
                _settings.BrokerHost, _settings.BrokerPort);
        }

        /// <summary>Subscribes to a topic filter with QoS 1.</summary>
        public async Task SubscribeAsync(string topicFilter, CancellationToken cancellationToken = default)
        {
            var subscribeOptions = new MqttFactory().CreateSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(topicFilter).WithAtLeastOnceQoS())
                .Build();

            await _mqttClient.SubscribeAsync(subscribeOptions, cancellationToken);
            _logger.LogInformation("MQTT: Subscribed to '{Topic}'.", topicFilter);
        }

        /// <inheritdoc/>
        public async Task PublishAsync(string topic, string payload, CancellationToken cancellationToken = default)
        {
            if (!_mqttClient.IsConnected)
            {
                _logger.LogWarning("MQTT: Publish skipped — client not connected. Topic='{Topic}'.", topic);
                return;
            }

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(Encoding.UTF8.GetBytes(payload))
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(false)
                .Build();

            await _mqttClient.PublishAsync(message, cancellationToken);
            _logger.LogInformation("MQTT: Published to '{Topic}'. PayloadLength={Len}.", topic, payload.Length);
        }

        /// <inheritdoc/>
        public Task PublishOtaMetadataAsync(string deviceId, string payload, CancellationToken cancellationToken = default)
            => PublishAsync(MqttTopics.UserAckPublish(deviceId), payload, cancellationToken);

        /// <summary>Gracefully disconnects from the broker.</summary>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_mqttClient.IsConnected)
            {
                await _mqttClient.DisconnectAsync(
                    new MqttClientDisconnectOptionsBuilder().Build(),
                    cancellationToken);
                _logger.LogInformation("MQTT: Disconnected from broker.");
            }
        }

        public async ValueTask DisposeAsync()
        {
            _mqttClient.DisconnectedAsync -= OnDisconnectedAsync;
            if (_mqttClient.IsConnected)
                await _mqttClient.DisconnectAsync();
            _mqttClient.Dispose();
        }

        // ── Private helpers ──────────────────────────────────────────────────────

        private MqttClientOptions BuildOptions()
        {
            var builder = new MqttClientOptionsBuilder()
                .WithTcpServer(_settings.BrokerHost, _settings.BrokerPort)
                .WithClientId(_settings.ClientId)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(_settings.KeepAliveSeconds))
                .WithCleanSession(true);

            if (!string.IsNullOrWhiteSpace(_settings.Username))
                builder.WithCredentials(_settings.Username, _settings.Password);

            if (_settings.UseTls)
                builder.WithTlsOptions(o => o.UseTls());

            return builder.Build();
        }

        private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
        {
            _logger.LogWarning("MQTT: Disconnected from broker. Reason={Reason}.", args.Reason);
            await Task.CompletedTask;
        }
    }
}
