namespace OTA.API.Models.Settings
{
    /// <summary>
    /// Configuration settings for the MQTT broker connection used by the OTA server.
    /// Bind from the "MqttSettings" section in appsettings.json.
    /// </summary>
    public sealed class MqttSettings
    {
        public const string SectionName = "MqttSettings";

        /// <summary>MQTT broker hostname or IP address.</summary>
        public string BrokerHost { get; set; } = "localhost";

        /// <summary>MQTT broker port (default 1883, TLS typically 8883).</summary>
        public int BrokerPort { get; set; } = 1883;

        /// <summary>Client identifier sent to the broker. Must be unique per connection.</summary>
        public string ClientId { get; set; } = "OTA-Server";

        /// <summary>Optional username for broker authentication.</summary>
        public string? Username { get; set; }

        /// <summary>Optional password for broker authentication.</summary>
        public string? Password { get; set; }

        /// <summary>Whether to use TLS/SSL for the broker connection.</summary>
        public bool UseTls { get; set; } = false;

        /// <summary>Seconds to wait before attempting to reconnect after a disconnection.</summary>
        public int ReconnectDelaySecs { get; set; } = 5;

        /// <summary>Connection keep-alive interval in seconds.</summary>
        public int KeepAliveSeconds { get; set; } = 60;

        /// <summary>
        /// When true the MQTT background service starts but logs a warning instead of failing
        /// if the broker is unreachable. Useful in dev when no broker is running.
        /// </summary>
        public bool OptionalInDevelopment { get; set; } = true;
    }
}
