using OTA.API.Models.Enums;

namespace OTA.API.Models.DTOs.Firmware
{
    public class FirmwareDto
    {
        public string FirmwareId { get; set; } = string.Empty;
        public string RepositoryId { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string GiteaTagName { get; set; } = string.Empty;
        public long GiteaReleaseId { get; set; }
        public string? ReleaseNotes { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileSha256 { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;
        public FirmwareChannel Channel { get; set; }
        public FirmwareStatus Status { get; set; }
        public bool IsMandate { get; set; }
        public List<string> SupportedModels { get; set; } = new();
        public List<string> SupportedHardwareRevisions { get; set; } = new();
        public string? MinRequiredVersion { get; set; }
        public DateTime? QaVerifiedAt { get; set; }
        public string? QaVerifiedByUserId { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedByUserId { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty;
    }
}
