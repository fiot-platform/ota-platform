using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OTA.API.Helpers;
using OTA.API.Models.DTOs.Notifications;
using OTA.API.Services.Interfaces;
using OTA.API.Models.Enums;

namespace OTA.API.Controllers
{
    /// <summary>
    /// Manages Firebase Cloud Messaging (FCM) token registration for push notifications
    /// and provides an ad-hoc send endpoint for administrators.
    /// </summary>
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    [Produces("application/json")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            INotificationService notificationService,
            ILogger<NotificationController> logger)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger              = logger               ?? throw new ArgumentNullException(nameof(logger));
        }

        private string CurrentUserId =>
            User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        private string CurrentUserRole =>
            User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? string.Empty;

        // ── Token registration ────────────────────────────────────────────────

        /// <summary>
        /// Registers or refreshes an FCM device token for the authenticated user.
        /// Call this after the Firebase SDK delivers a new registration token on the client.
        /// </summary>
        [HttpPost("tokens")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterToken(
            [FromBody] RegisterFcmTokenRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request?.Token))
                return BadRequest(ApiResponse<object>.Fail("Token must not be empty."));

            await _notificationService.RegisterTokenAsync(
                CurrentUserId, request.Token, request.DeviceLabel, cancellationToken);

            return Ok(ApiResponse<object>.Ok(null, "FCM token registered successfully."));
        }

        /// <summary>
        /// Removes a previously registered FCM token for the authenticated user.
        /// Call this when the user logs out from a specific device.
        /// </summary>
        [HttpDelete("tokens")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnregisterToken(
            [FromBody] UnregisterFcmTokenRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request?.Token))
                return BadRequest(ApiResponse<object>.Fail("Token must not be empty."));

            await _notificationService.UnregisterTokenAsync(
                CurrentUserId, request.Token, cancellationToken);

            return Ok(ApiResponse<object>.Ok(null, "FCM token unregistered successfully."));
        }

        /// <summary>
        /// Returns all FCM tokens registered for the authenticated user.
        /// </summary>
        [HttpGet("tokens")]
        [ProducesResponseType(typeof(ApiResponse<List<FcmTokenDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyTokens(CancellationToken cancellationToken = default)
        {
            var tokens = await _notificationService.GetUserTokensAsync(CurrentUserId, cancellationToken);
            return Ok(ApiResponse<List<FcmTokenDto>>.Ok(tokens, $"{tokens.Count} token(s) registered."));
        }

        // ── In-app notification inbox ─────────────────────────────────────────

        /// <summary>
        /// Returns the in-app notification inbox for the authenticated user (role-filtered).
        /// </summary>
        [HttpGet("inbox")]
        [ProducesResponseType(typeof(ApiResponse<InboxResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInbox(
            [FromQuery] int limit = 50,
            CancellationToken cancellationToken = default)
        {
            var role = CurrentUserRole;
            var inbox = await _notificationService.GetInboxAsync(CurrentUserId, role, Math.Min(limit, 100), cancellationToken);
            return Ok(ApiResponse<InboxResponse>.Ok(inbox));
        }

        /// <summary>
        /// Marks a single notification as read for the authenticated user.
        /// </summary>
        [HttpPatch("inbox/{id}/read")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkAsRead(string id, CancellationToken cancellationToken = default)
        {
            await _notificationService.MarkAsReadAsync(id, CurrentUserId, cancellationToken);
            return Ok(ApiResponse<object>.Ok(null, "Marked as read."));
        }

        /// <summary>
        /// Marks all notifications in the user's inbox as read.
        /// </summary>
        [HttpPost("inbox/read-all")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken = default)
        {
            await _notificationService.MarkAllAsReadAsync(CurrentUserId, CurrentUserRole, cancellationToken);
            return Ok(ApiResponse<object>.Ok(null, "All notifications marked as read."));
        }

        // ── Ad-hoc send (admin only) ──────────────────────────────────────────

        /// <summary>
        /// Sends a custom push notification to a list of FCM tokens or an FCM topic.
        /// Requires SuperAdmin or PlatformAdmin role.
        /// </summary>
        [HttpPost("send")]
        [Authorize(Policy = "CanManageUsers")]
        [ProducesResponseType(typeof(ApiResponse<NotificationResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SendNotification(
            [FromBody] SendNotificationRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request?.Title))
                return BadRequest(ApiResponse<object>.Fail("Title is required."));
            if (string.IsNullOrWhiteSpace(request.Body))
                return BadRequest(ApiResponse<object>.Fail("Body is required."));

            bool hasTokens = request.Tokens?.Any() == true;
            bool hasTopic  = !string.IsNullOrWhiteSpace(request.Topic);

            if (!hasTokens && !hasTopic)
                return BadRequest(ApiResponse<object>.Fail("Either Tokens or Topic must be provided."));
            if (hasTokens && hasTopic)
                return BadRequest(ApiResponse<object>.Fail("Provide either Tokens or Topic, not both."));

            NotificationResultDto result;

            if (hasTopic)
            {
                result = await _notificationService.SendToTopicAsync(
                    request.Topic!, request.Title, request.Body, request.Data, cancellationToken);
            }
            else
            {
                result = await _notificationService.SendToTokensAsync(
                    request.Tokens!, request.Title, request.Body, request.Data, cancellationToken);
            }

            _logger.LogInformation(
                "Ad-hoc notification sent by {UserId}: title={Title} success={S} failure={F}",
                CurrentUserId, request.Title, result.SuccessCount, result.FailureCount);

            return Ok(ApiResponse<NotificationResultDto>.Ok(result, result.Message));
        }
    }
}
