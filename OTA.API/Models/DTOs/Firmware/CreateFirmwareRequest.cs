using System.ComponentModel.DataAnnotations;
using OTA.API.Models.Enums;

namespace OTA.API.Models.DTOs.Firmware
{
    public class CreateFirmwareRequest
    {
        [Required]
        public string RepositoryId { get; set; } = string.Empty;

        [Required]
        public string Version { get; set; } = string.Empty;

        [Required]
        public string GiteaTagName { get; set; } = string.Empty;

        public long GiteaReleaseId { get; set; }

        public string? ReleaseNotes { get; set; }

        public string? FileName { get; set; }

        public string? FileSha256 { get; set; }

        public long FileSizeBytes { get; set; }

        public string? DownloadUrl { get; set; }

        public FirmwareChannel Channel { get; set; } = FirmwareChannel.Alpha;

        public List<string> SupportedModels { get; set; } = new();

        public List<string> SupportedHardwareRevisions { get; set; } = new();

        public string? MinRequiredVersion { get; set; }

        public bool IsMandate { get; set; } = false;
    }
}
