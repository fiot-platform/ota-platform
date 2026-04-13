using System.ComponentModel.DataAnnotations;

namespace OTA.API.Models.DTOs.Projects
{
    public class UpdateProjectRequest
    {
        [MaxLength(200)]
        public string? Name { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public string? BusinessUnit { get; set; }

        public string? GiteaOrgName { get; set; }

        public List<string>? Tags { get; set; }
    }
}
