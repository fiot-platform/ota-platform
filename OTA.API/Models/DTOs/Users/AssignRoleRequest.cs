using System.ComponentModel.DataAnnotations;
using OTA.API.Models.Enums;

namespace OTA.API.Models.DTOs.Users
{
    public class AssignRoleRequest
    {
        [Required]
        public UserRole Role { get; set; }
    }
}
