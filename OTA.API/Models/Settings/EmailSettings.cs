namespace OTA.API.Models.Settings
{
    /// <summary>
    /// SMTP email configuration loaded from appsettings.json → EmailSettings section.
    /// </summary>
    public sealed class EmailSettings
    {
        public const string SectionName = "EmailSettings";

        /// <summary>SMTP server host, e.g. smtp.office365.com</summary>
        public string Host { get; set; } = string.Empty;

        /// <summary>SMTP port (587 for STARTTLS, 465 for SSL).</summary>
        public int Port { get; set; } = 587;

        /// <summary>SMTP authentication username (usually the sender email).</summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>SMTP authentication password.</summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>The From address used in outgoing emails.</summary>
        public string SenderEmail { get; set; } = string.Empty;

        /// <summary>The display name shown in the From field.</summary>
        public string SenderName { get; set; } = "OTA Platform";

        /// <summary>Whether to use SSL/TLS (true) or STARTTLS (false).</summary>
        public bool UseSsl { get; set; } = false;

        /// <summary>Whether email sending is enabled. Set to false to disable without removing config.</summary>
        public bool Enabled { get; set; } = true;
    }
}
