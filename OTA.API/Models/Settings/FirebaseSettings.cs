namespace OTA.API.Models.Settings
{
    /// <summary>
    /// Configuration settings for Firebase Cloud Messaging (FCM).
    /// Bind from the "FirebaseSettings" section in appsettings.json.
    /// </summary>
    public sealed class FirebaseSettings
    {
        public const string SectionName = "FirebaseSettings";

        /// <summary>
        /// Absolute or relative path to the Firebase service account JSON key file.
        /// This file is downloaded from the Firebase console under Project Settings → Service Accounts.
        /// Example: "/etc/secrets/firebase-service-account.json"
        /// </summary>
        public string ServiceAccountKeyPath { get; set; } = string.Empty;

        /// <summary>
        /// Firebase project ID (found in Project Settings → General).
        /// Used to validate the FCM sender identity.
        /// </summary>
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>
        /// When true the notification service is active and will dispatch FCM messages.
        /// Set to false in development environments without a valid service account file.
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}
