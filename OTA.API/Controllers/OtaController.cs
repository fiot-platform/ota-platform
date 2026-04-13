using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OTA.API.Helpers;
using OTA.API.Models.DTOs;
using OTA.API.Services.Interfaces;

namespace OTA.API.Controllers
{
    /// <summary>
    /// OTA rollout lifecycle management. Supports creating, starting, pausing, resuming, and cancelling
    /// rollouts, querying jobs, retrying failed jobs, and retrieving rollout summaries.
    /// </summary>
    [ApiController]
    [Route("api/rollouts")]
    [Authorize]
    [Produces("application/json")]
    public class OtaController : ControllerBase
    {
        private readonly IOtaService _otaService;
        private readonly ILogger<OtaController> _logger;

        public OtaController(IOtaService otaService, ILogger<OtaController> logger)
        {
            _otaService = otaService ?? throw new ArgumentNullException(nameof(otaService));
            _logger     = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private string CurrentUserId =>
            User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        private string CurrentEmail =>
            User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? string.Empty;

        private string ClientIp =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

        /// <summary>Returns a paginated list of rollouts. Requires CanManageRollouts.</summary>
        [HttpGet]
        [Authorize(Policy = "CanManageRollouts")]
        [ProducesResponseType(typeof(ApiResponse<List<RolloutDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRollouts(
            [FromQuery] string? filter = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            var result = await _otaService.GetRolloutsAsync(filter ?? string.Empty, page, pageSize, cancellationToken);
            var pagination = PaginationInfo.Create(page, pageSize, result.TotalCount);
            return Ok(ApiResponse<List<RolloutDto>>.Ok(result.Items, "Rollouts retrieved successfully.", pagination));
        }

        /// <summary>Returns a single rollout by its identifier. Requires CanManageRollouts.</summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "CanManageRollouts")]
        [ProducesResponseType(typeof(ApiResponse<RolloutDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRolloutById(string id, CancellationToken cancellationToken = default)
        {
            var rollout = await _otaService.GetRolloutByIdAsync(id, cancellationToken);
            if (rollout == null)
                return NotFound(ApiResponse<RolloutDto>.Fail($"Rollout '{id}' was not found."));

            return Ok(ApiResponse<RolloutDto>.Ok(rollout));
        }

        /// <summary>Returns a progress summary for a rollout. Requires CanManageRollouts.</summary>
        [HttpGet("{id}/summary")]
        [Authorize(Policy = "CanManageRollouts")]
        [ProducesResponseType(typeof(ApiResponse<RolloutSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRolloutSummary(string id, CancellationToken cancellationToken = default)
        {
            var summary = await _otaService.GetRolloutSummaryAsync(id, cancellationToken);
            return Ok(ApiResponse<RolloutSummaryDto>.Ok(summary));
        }

        /// <summary>Returns all OTA jobs belonging to a rollout. Requires CanManageRollouts.</summary>
        [HttpGet("{id}/jobs")]
        [Authorize(Policy = "CanManageRollouts")]
        [ProducesResponseType(typeof(ApiResponse<List<OtaJobDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRolloutJobs(string id, CancellationToken cancellationToken = default)
        {
            var jobs = await _otaService.GetRolloutJobsAsync(id, cancellationToken);
            return Ok(ApiResponse<List<OtaJobDto>>.Ok(jobs, $"{jobs.Count} job(s) retrieved for rollout '{id}'."));
        }

        /// <summary>Creates a new rollout in Draft status. Requires CanManageRollouts.</summary>
        [HttpPost]
        [Authorize(Policy = "CanManageRollouts")]
        [ProducesResponseType(typeof(ApiResponse<RolloutDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateRollout(
            [FromBody] CreateRolloutRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<RolloutDto>.Fail("Validation failed.", errors));
            }

            var created = await _otaService.CreateRolloutAsync(request, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
            return CreatedAtAction(nameof(GetRolloutById), new { id = created.RolloutId },
                ApiResponse<RolloutDto>.Ok(created, "Rollout created successfully."));
        }

        /// <summary>Starts a Draft rollout, transitioning it to Active. Requires CanControlRollouts.</summary>
        [HttpPost("{id}/start")]
        [Authorize(Policy = "CanControlRollouts")]
        [ProducesResponseType(typeof(ApiResponse<RolloutDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> StartRollout(string id, CancellationToken cancellationToken = default)
        {
            var result = await _otaService.StartRolloutAsync(id, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
            return Ok(ApiResponse<RolloutDto>.Ok(result, "Rollout started successfully."));
        }

        /// <summary>Pauses an Active rollout. Requires CanControlRollouts.</summary>
        [HttpPost("{id}/pause")]
        [Authorize(Policy = "CanControlRollouts")]
        [ProducesResponseType(typeof(ApiResponse<RolloutDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PauseRollout(string id, CancellationToken cancellationToken = default)
        {
            var result = await _otaService.PauseRolloutAsync(id, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
            return Ok(ApiResponse<RolloutDto>.Ok(result, "Rollout paused."));
        }

        /// <summary>Resumes a Paused rollout. Requires CanControlRollouts.</summary>
        [HttpPost("{id}/resume")]
        [Authorize(Policy = "CanControlRollouts")]
        [ProducesResponseType(typeof(ApiResponse<RolloutDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResumeRollout(string id, CancellationToken cancellationToken = default)
        {
            var result = await _otaService.ResumeRolloutAsync(id, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
            return Ok(ApiResponse<RolloutDto>.Ok(result, "Rollout resumed."));
        }

        /// <summary>Cancels an Active or Paused rollout. Requires CanControlRollouts.</summary>
        [HttpPost("{id}/cancel")]
        [Authorize(Policy = "CanControlRollouts")]
        [ProducesResponseType(typeof(ApiResponse<RolloutDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelRollout(string id, CancellationToken cancellationToken = default)
        {
            var result = await _otaService.CancelRolloutAsync(id, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
            return Ok(ApiResponse<RolloutDto>.Ok(result, "Rollout cancelled."));
        }

        /// <summary>Retries a failed OTA job. Requires CanControlRollouts.</summary>
        [HttpPost("jobs/{jobId}/retry")]
        [Authorize(Policy = "CanControlRollouts")]
        [ProducesResponseType(typeof(ApiResponse<OtaJobDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RetryJob(string jobId, CancellationToken cancellationToken = default)
        {
            var result = await _otaService.RetryJobAsync(jobId, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
            return Ok(ApiResponse<OtaJobDto>.Ok(result, "OTA job queued for retry."));
        }
    }
}
