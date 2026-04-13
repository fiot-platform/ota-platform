using OTA.API.Models.Enums;

namespace OTA.API.Models.DTOs.Firmware
{
    public class QAVerifyRequest
    {
        public string? Remarks { get; set; }
    }

    public class RejectFirmwareRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class AssignChannelRequest
    {
        public FirmwareChannel Channel { get; set; }
    }

    public class UpdateFirmwareRequest
    {
        public string? ReleaseNotes { get; set; }
        public bool? IsMandate { get; set; }
        public List<string>? SupportedModels { get; set; }
        public List<string>? SupportedHardwareRevisions { get; set; }
        public string? MinRequiredVersion { get; set; }
    }
}
