using System.ComponentModel.DataAnnotations;

namespace OTA.API.Models.DTOs.Users
{
    public class UpdateUserRequest
    {
        [MaxLength(100)]
        public string? Name { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public string? CustomerId { get; set; }

        public List<string>? ProjectScope { get; set; }
    }
}
