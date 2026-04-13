using System.ComponentModel.DataAnnotations;

namespace OTA.API.Models.DTOs.Repositories
{
    public class RegisterRepositoryRequest
    {
        [Required]
        public string ProjectId { get; set; } = string.Empty;

        [Required]
        public long GiteaRepoId { get; set; }

        [Required]
        public string GiteaRepoName { get; set; } = string.Empty;

        [Required]
        public string GiteaOwner { get; set; } = string.Empty;

        public string GiteaCloneUrl { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string DefaultBranch { get; set; } = "main";

        /// <summary>When true the repository is created as private on Gitea. Ignored if the repo already exists.</summary>
        public bool IsPrivate { get; set; } = false;

        public List<string> Topics { get; set; } = new();
    }
}
