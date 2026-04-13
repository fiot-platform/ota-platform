using System.ComponentModel.DataAnnotations;
using OTA.API.Models.Enums;

namespace OTA.API.Models.DTOs.OTA
{
    public class CreateRolloutRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public string ProjectId { get; set; } = string.Empty;

        [Required]
        public string FirmwareId { get; set; } = string.Empty;

        public TargetType TargetType { get; set; } = TargetType.AllDevices;

        public List<string> TargetIds { get; set; } = new();

        public string? Channel { get; set; }

        public string? PolicyId { get; set; }

        public RolloutPhase Phase { get; set; } = RolloutPhase.Full;

        public DateTime? ScheduledAt { get; set; }

        public string? Notes { get; set; }
    }
}
