namespace OTA.API.Models.DTOs.Repositories
{
    public class RepositoryDto
    {
        public string RepositoryId { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public long GiteaRepoId { get; set; }
        public string GiteaRepoName { get; set; } = string.Empty;
        public string GiteaOwner { get; set; } = string.Empty;
        public string GiteaCloneUrl { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastSyncedAt { get; set; }
        public string DefaultBranch { get; set; } = "main";
        public List<string> Topics { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
