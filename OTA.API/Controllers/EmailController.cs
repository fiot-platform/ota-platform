using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OTA.API.Helpers;
using OTA.API.Models.DTOs;
using OTA.API.Models.Settings;
using OTA.API.Services.Interfaces;

namespace OTA.API.Controllers
{
    /// <summary>
    /// Email configuration and test-send endpoints.
    /// Only SuperAdmin and PlatformAdmin can access these.
    /// </summary>
    [ApiController]
    [Route("api/email")]
    [Authorize(Policy = "CanManageUsers")]   // restrict to admin roles
    [Produces("application/json")]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailController> _logger;

        public EmailController(
            IEmailService emailService,
            IOptions<EmailSettings> settings,
            ILogger<EmailController> logger)
        {
            _emailService = emailService;
            _settings     = settings.Value;
            _logger       = logger;
        }

        /// <summary>Returns the current email configuration (password masked).</summary>
        [HttpGet("config")]
        [ProducesResponseType(typeof(ApiResponse<EmailConfigDto>), StatusCodes.Status200OK)]
        public IActionResult GetConfig()
        {
            var dto = new EmailConfigDto
            {
                Host        = _settings.Host,
                Port        = _settings.Port,
                UserName    = _settings.UserName,
                SenderEmail = _settings.SenderEmail,
                SenderName  = _settings.SenderName,
                UseSsl      = _settings.UseSsl,
                Enabled     = _settings.Enabled
            };
            return Ok(ApiResponse<EmailConfigDto>.Ok(dto, "Email configuration retrieved."));
        }

        /// <summary>Sends a test email to the specified address to verify SMTP connectivity.</summary>
        [HttpPost("test")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SendTestEmail(
            [FromBody] SendTestEmailRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.ToEmail))
                return BadRequest(ApiResponse<object>.Fail("Recipient email is required."));

            try
            {
                await _emailService.SendAsync(
                    request.ToEmail,
                    request.ToName ?? "Test Recipient",
                    "[OTA Platform] SMTP Test Email",
                    "<h2>SMTP Test Successful ✔</h2><p>This confirms your OTA Platform email configuration is working correctly.</p>",
                    cancellationToken);

                _logger.LogInformation("Test email sent to {Email} by {User}.", request.ToEmail, User.Identity?.Name);
                return Ok(ApiResponse<object>.Ok(null, $"Test email sent to {request.ToEmail}."));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Test email to {Email} failed.", request.ToEmail);
                return StatusCode(502, ApiResponse<object>.Fail($"SMTP error: {ex.Message}"));
            }
        }
    }

    public sealed class EmailConfigDto
    {
        public string Host        { get; set; } = string.Empty;
        public int    Port        { get; set; }
        public string UserName    { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName  { get; set; } = string.Empty;
        public bool   UseSsl      { get; set; }
        public bool   Enabled     { get; set; }
    }

    public sealed class SendTestEmailRequest
    {
        public string ToEmail { get; set; } = string.Empty;
        public string? ToName { get; set; }
    }
}
