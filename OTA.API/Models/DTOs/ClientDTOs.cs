using System.ComponentModel.DataAnnotations;

namespace OTA.API.Models.DTOs
{
    // ─────────────────────────────────────────────────────────────────────────
    // Client DTOs
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Request body for creating a new client organisation.</summary>
    public sealed class CreateClientRequest
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Short unique code used as CustomerCode in device registration (e.g., "ACME").</summary>
        [Required(ErrorMessage = "Code is required.")]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(200)]
        [EmailAddress(ErrorMessage = "ContactEmail must be a valid e-mail address.")]
        public string? ContactEmail { get; set; }

        [MaxLength(50)]
        public string? ContactPhone { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }

    /// <summary>Request body for updating mutable client attributes.</summary>
    public sealed class UpdateClientRequest
    {
        [MaxLength(200)]
        public string? Name { get; set; }

        [MaxLength(50)]
        public string? Code { get; set; }

        [MaxLength(200)]
        [EmailAddress(ErrorMessage = "ContactEmail must be a valid e-mail address.")]
        public string? ContactEmail { get; set; }

        [MaxLength(50)]
        public string? ContactPhone { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public bool? IsActive { get; set; }
    }

    /// <summary>Summary row for paginated client list responses.</summary>
    public sealed class ClientListDto
    {
        public string Id { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Full client detail returned by GET /api/clients/{id}.</summary>
    public class ClientDetailDto
    {
        public string Id { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedByUserId { get; set; }
    }

    /// <summary>Wrapper for paginated client list responses.</summary>
    public sealed class PagedClientListResponse
    {
        public List<ClientListDto> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public long TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
