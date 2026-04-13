namespace OTA.API.Models.DTOs.Repositories
{
    public class UpdateRepositoryRequest
    {
        public string? Description { get; set; }
        public string? DefaultBranch { get; set; }
        public List<string>? Topics { get; set; }
    }
}
