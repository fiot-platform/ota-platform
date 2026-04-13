using OTA.API.Models.Enums;

namespace OTA.API.Models.DTOs.Users
{
    public class UserDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string RoleName => Role.ToString();
        public bool IsActive { get; set; }
        public string? CustomerId { get; set; }
        public List<string> ProjectScope { get; set; } = new();
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
