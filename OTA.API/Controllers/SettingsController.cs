using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OTA.API.Helpers;
using OTA.API.Models.DTOs;
using OTA.API.Models.Entities;
using OTA.API.Repositories.Interfaces;

namespace OTA.API.Controllers
{
    /// <summary>
    /// Platform-level settings endpoints. Only SuperAdmin and PlatformAdmin may access these.
    /// </summary>
    [ApiController]
    [Route("api/settings")]
    [Authorize(Policy = "CanManageUsers")]
    [Produces("application/json")]
    public class SettingsController : ControllerBase
    {
        private readonly IEmailNotificationSettingsRepository _settingsRepo;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(
            IEmailNotificationSettingsRepository settingsRepo,
            ILogger<SettingsController> logger)
        {
            _settingsRepo = settingsRepo ?? throw new ArgumentNullException(nameof(settingsRepo));
            _logger       = logger       ?? throw new ArgumentNullException(nameof(logger));
        }

        private string CurrentEmail =>
            User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? string.Empty;

        // ── GET /api/settings/email-notifications ─────────────────────────────

        /// <summary>
        /// Returns the current email notification settings.
        /// If no settings have been saved yet, platform defaults are returned.
        /// </summary>
        [HttpGet("email-notifications")]
        [ProducesResponseType(typeof(ApiResponse<EmailNotificationSettingsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEmailNotificationSettings(CancellationToken cancellationToken = default)
        {
            var entity = await _settingsRepo.GetAsync(cancellationToken);

            // Return defaults if nothing is stored yet
            entity ??= new EmailNotificationSettingsEntity();

            var dto = MapToDto(entity);
            return Ok(ApiResponse<EmailNotificationSettingsDto>.Ok(dto, "Email notification settings retrieved."));
        }

        // ── PUT /api/settings/email-notifications ─────────────────────────────

        /// <summary>
        /// Saves (upserts) email notification settings and returns the persisted state.
        /// </summary>
        [HttpPut("email-notifications")]
        [ProducesResponseType(typeof(ApiResponse<EmailNotificationSettingsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateEmailNotificationSettings(
            [FromBody] UpdateEmailNotificationSettingsRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                return BadRequest(ApiResponse<object>.Fail("Request body is required."));

            // Load existing document so we preserve the Id (for upsert key stability)
            var existing = await _settingsRepo.GetAsync(cancellationToken);

            var entity = new EmailNotificationSettingsEntity
            {
                Id                  = existing?.Id ?? string.Empty,
                OnFirmwareSubmitted  = request.OnFirmwareSubmitted,
                OnFirmwareApproved   = request.OnFirmwareApproved,
                OnFirmwareRejected   = request.OnFirmwareRejected,
                OnFirmwareQAVerified = request.OnFirmwareQAVerified,
                OnRolloutStarted     = request.OnRolloutStarted,
                OnRolloutCompleted   = request.OnRolloutCompleted,
                OnRolloutFailed      = request.OnRolloutFailed,
                OnDeviceOtaFailed    = request.OnDeviceOtaFailed,
                OnDeviceRegistered   = request.OnDeviceRegistered,
                OnNewUserCreated     = request.OnNewUserCreated,
                OnUserDeactivated    = request.OnUserDeactivated,
                NotifyEmails         = request.NotifyEmails ?? new List<string>(),
                UpdatedAt            = DateTime.UtcNow,
                UpdatedBy            = CurrentEmail,
            };

            await _settingsRepo.SaveAsync(entity, cancellationToken);

            _logger.LogInformation(
                "Email notification settings updated by {User}.", CurrentEmail);

            var dto = MapToDto(entity);
            return Ok(ApiResponse<EmailNotificationSettingsDto>.Ok(dto, "Email notification settings saved successfully."));
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static EmailNotificationSettingsDto MapToDto(EmailNotificationSettingsEntity e) =>
            new()
            {
                OnFirmwareSubmitted  = e.OnFirmwareSubmitted,
                OnFirmwareApproved   = e.OnFirmwareApproved,
                OnFirmwareRejected   = e.OnFirmwareRejected,
                OnFirmwareQAVerified = e.OnFirmwareQAVerified,
                OnRolloutStarted     = e.OnRolloutStarted,
                OnRolloutCompleted   = e.OnRolloutCompleted,
                OnRolloutFailed      = e.OnRolloutFailed,
                OnDeviceOtaFailed    = e.OnDeviceOtaFailed,
                OnDeviceRegistered   = e.OnDeviceRegistered,
                OnNewUserCreated     = e.OnNewUserCreated,
                OnUserDeactivated    = e.OnUserDeactivated,
                NotifyEmails         = e.NotifyEmails,
                UpdatedAt            = e.UpdatedAt == default ? null : e.UpdatedAt,
                UpdatedBy            = string.IsNullOrWhiteSpace(e.UpdatedBy) ? null : e.UpdatedBy,
            };
    }
}
