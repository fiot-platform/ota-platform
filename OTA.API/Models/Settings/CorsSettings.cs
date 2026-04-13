namespace OTA.API.Models.Settings
{
    /// <summary>
    /// Configuration settings for CORS policy.
    /// </summary>
    public class CorsSettings
    {
        public const string SectionName = "CorsSettings";

        /// <summary>
        /// List of allowed frontend origins for cross-origin requests.
        /// </summary>
        public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    }
}
