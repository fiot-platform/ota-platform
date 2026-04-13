using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OTA.API.Helpers;
using OTA.API.Models.DTOs;
using OTA.API.Models.Settings;
using OTA.API.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace OTA.API.Controllers
{
    /// <summary>
    /// Receives incoming Gitea webhook events, validates HMAC-SHA256 signatures using the shared secret,
    /// and delegates processing to <see cref="IGiteaWebhookService"/>.
    /// All POST endpoints are unauthenticated (Gitea calls them directly).
    /// Management endpoints require SuperAdmin or PlatformAdmin.
    /// </summary>
    [ApiController]
    [Route("api/webhooks/gitea")]
    [Produces("application/json")]
    public class GiteaWebhookController : ControllerBase
    {
        private readonly IGiteaWebhookService _webhookService;
        private readonly GiteaSettings _giteaSettings;
        private readonly ILogger<GiteaWebhookController> _logger;

        private const string GiteaEventHeader    = "X-Gitea-Event";
        private const string GiteaDeliveryHeader = "X-Gitea-Delivery";
        private const string GiteaSignatureHeader = "X-Gitea-Signature";

        public GiteaWebhookController(
            IGiteaWebhookService webhookService,
            IOptions<GiteaSettings> giteaSettings,
            ILogger<GiteaWebhookController> logger)
        {
            _webhookService = webhookService ?? throw new ArgumentNullException(nameof(webhookService));
            _giteaSettings  = giteaSettings?.Value ?? throw new ArgumentNullException(nameof(giteaSettings));
            _logger         = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Main webhook receiver. Gitea POSTs here when a repository event occurs.
        /// Validates the HMAC-SHA256 signature before processing.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ReceiveWebhook(CancellationToken cancellationToken = default)
        {
            // Read raw body for signature validation (must happen before model binding)
            string payload;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
            {
                payload = await reader.ReadToEndAsync(cancellationToken);
            }

            // Extract Gitea-specific headers
            var eventType  = Request.Headers[GiteaEventHeader].FirstOrDefault();
            var deliveryId = Request.Headers[GiteaDeliveryHeader].FirstOrDefault();
            var signature  = Request.Headers[GiteaSignatureHeader].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(eventType))
                return BadRequest(ApiResponse.Fail("Missing required header: X-Gitea-Event."));

            if (string.IsNullOrWhiteSpace(deliveryId))
                return BadRequest(ApiResponse.Fail("Missing required header: X-Gitea-Delivery."));

            // Validate HMAC-SHA256 signature when a webhook secret is configured
            if (!string.IsNullOrWhiteSpace(_giteaSettings.WebhookSecret))
            {
                if (string.IsNullOrWhiteSpace(signature))
                {
                    _logger.LogWarning("Gitea webhook received without signature. DeliveryId={DeliveryId}.", deliveryId);
                    return Unauthorized(ApiResponse.Fail("Missing webhook signature."));
                }

                if (!ValidateHmacSha256Signature(payload, signature, _giteaSettings.WebhookSecret))
                {
                    _logger.LogWarning("Gitea webhook signature validation failed. DeliveryId={DeliveryId}.", deliveryId);
                    return Unauthorized(ApiResponse.Fail("Webhook signature validation failed."));
                }
            }

            _logger.LogInformation("Gitea webhook received. Event={Event}, Delivery={Delivery}.", eventType, deliveryId);

            var processed = await _webhookService.ProcessWebhookAsync(payload, eventType, deliveryId, cancellationToken);

            return Ok(ApiResponse<object>.Ok(
                new { Processed = processed, DeliveryId = deliveryId },
                processed ? "Webhook processed successfully." : "Webhook received but not processed (event skipped)."));
        }

        /// <summary>Returns recent webhook events with optional filtering. SuperAdmin and PlatformAdmin only.</summary>
        [HttpGet("events")]
        [Authorize(Roles = "SuperAdmin,PlatformAdmin")]
        [ProducesResponseType(typeof(ApiResponse<List<RepositoryEventDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRecentEvents(
            [FromQuery] string? giteaRepoId = null,
            [FromQuery] string? status = null,
            [FromQuery] int? limit = null,
            CancellationToken cancellationToken = default)
        {
            var filter = new RepositoryEventFilterRequest
            {
                GiteaRepoId = giteaRepoId,
                Limit       = limit ?? 20
            };

            var events = await _webhookService.GetRecentEventsAsync(filter, cancellationToken);
            return Ok(ApiResponse<List<RepositoryEventDto>>.Ok(events, $"{events.Count} recent event(s) retrieved."));
        }

        /// <summary>Reprocesses a previously failed webhook event. SuperAdmin and PlatformAdmin only.</summary>
        [HttpPost("events/{eventId}/reprocess")]
        [Authorize(Roles = "SuperAdmin,PlatformAdmin")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ReprocessEvent(string eventId, CancellationToken cancellationToken = default)
        {
            await _webhookService.ReprocessEventAsync(eventId, cancellationToken);
            return Ok(ApiResponse.OkNoData($"Webhook event '{eventId}' queued for reprocessing."));
        }

        // ── Private helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Validates the Gitea HMAC-SHA256 webhook signature using constant-time comparison.
        /// Gitea sends the signature as a hex string (no "sha256=" prefix — unlike GitHub).
        /// </summary>
        private static bool ValidateHmacSha256Signature(string payload, string signature, string secret)
        {
            try
            {
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
                var computed    = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var computedHex = Convert.ToHexString(computed).ToLowerInvariant();

                // Gitea may send with or without "sha256=" prefix; strip it for comparison
                var incoming = signature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase)
                    ? signature["sha256=".Length..]
                    : signature;

                return CryptographicEqual(computedHex, incoming.ToLowerInvariant());
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Constant-time string comparison to prevent timing attacks.</summary>
        private static bool CryptographicEqual(string a, string b)
        {
            if (a.Length != b.Length) return false;
            int result = 0;
            for (int i = 0; i < a.Length; i++)
                result |= a[i] ^ b[i];
            return result == 0;
        }
    }
}
