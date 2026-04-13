using System.ComponentModel.DataAnnotations;

namespace OTA.API.Models.DTOs.Devices
{
    public class RegisterDeviceRequest
    {
        [Required]
        public string SerialNumber { get; set; } = string.Empty;

        [Required]
        public string Model { get; set; } = string.Empty;

        public string? HardwareRevision { get; set; }

        [Required]
        public string CustomerId { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string? SiteId { get; set; }

        public string? SiteName { get; set; }

        public string? CurrentFirmwareVersion { get; set; }

        public List<string> Tags { get; set; } = new();

        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
