using System.ComponentModel.DataAnnotations;

namespace OTA.API.Models.DTOs.Projects
{
    public class CreateProjectRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public string CustomerId { get; set; } = string.Empty;

        [Required]
        public string CustomerName { get; set; } = string.Empty;

        public string? BusinessUnit { get; set; }

        public string? GiteaOrgName { get; set; }

        public List<string> Tags { get; set; } = new();
    }
}
