using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OTA.API.Helpers;
using OTA.API.Models.DTOs;
using OTA.API.Services.Interfaces;

namespace OTA.API.Controllers
{
    /// <summary>
    /// QA session management for firmware versions.
    /// All routes are scoped under /api/qa/firmware/{firmwareId}.
    /// </summary>
    [ApiController]
    [Route("api/qa/firmware/{firmwareId}")]
    [Authorize]
    [Produces("application/json")]
    public class QAController : ControllerBase
    {
        private readonly IQAService _qaService;
        private readonly IUserService _userService;
        private readonly ILogger<QAController> _logger;

        public QAController(IQAService qaService, IUserService userService, ILogger<QAController> logger)
        {
            _qaService   = qaService;
            _userService = userService;
            _logger      = logger;
        }

        private string CurrentUserId =>
            User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        private string CurrentName =>
            User.FindFirstValue("fullName") ?? User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        private string BaseUrl =>
            $"{Request.Scheme}://{Request.Host}";

        /// <summary>Returns the QA session for the given firmware version. Returns 404 if not yet started.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<QASessionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSession(string firmwareId, CancellationToken cancellationToken = default)
        {
            var session = await _qaService.GetSessionAsync(firmwareId, cancellationToken);
            if (session is null)
                return NotFound(ApiResponse<QASessionDto>.Fail($"No QA session found for firmware '{firmwareId}'."));

            // Enrich StartedByName for sessions that predate the denormalised name field.
            if (string.IsNullOrWhiteSpace(session.StartedByName) && !string.IsNullOrWhiteSpace(session.StartedByUserId))
            {
                try
                {
                    var user = await _userService.GetUserByIdAsync(session.StartedByUserId, cancellationToken);
                    if (user != null)
                        session.StartedByName = !string.IsNullOrWhiteSpace(user.Name) ? user.Name : user.Email;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not enrich QA session starter name for userId={UserId}", session.StartedByUserId);
                }
            }

            return Ok(ApiResponse<QASessionDto>.Ok(session));
        }

        /// <summary>Starts a new QA session for the firmware version. QA engineers only.</summary>
        [HttpPost("start")]
        [Authorize(Policy = "CanRunQASession")]
        [ProducesResponseType(typeof(ApiResponse<QASessionDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> StartSession(string firmwareId, CancellationToken cancellationToken = default)
        {
            var session = await _qaService.StartSessionAsync(firmwareId, CurrentUserId, CurrentName, cancellationToken);
            return StatusCode(201, ApiResponse<QASessionDto>.Ok(session, "QA session started."));
        }

        /// <summary>Updates the QA session status (InProgress, BugListRaised, Complete, Fail).</summary>
        [HttpPut("status")]
        [Authorize(Policy = "CanRunQASession")]
        [ProducesResponseType(typeof(ApiResponse<QASessionDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateStatus(string firmwareId, [FromBody] UpdateQAStatusRequest request, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<QASessionDto>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

            var session = await _qaService.UpdateStatusAsync(firmwareId, request, CurrentUserId, cancellationToken);
            return Ok(ApiResponse<QASessionDto>.Ok(session, "QA status updated."));
        }

        /// <summary>
        /// Uploads a test case or test result document (multipart/form-data).
        /// Use ?type=testCase or ?type=testResult (default: testCase).
        /// Max 50 MB per file.
        /// </summary>
        [HttpPost("documents")]
        [Authorize(Policy = "CanRunQASession")]
        [RequestSizeLimit(52_428_800)]
        [RequestFormLimits(MultipartBodyLengthLimit = 52_428_800)]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<UploadQADocumentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadDocument(
            string firmwareId,
            IFormFile file,
            [FromQuery] string type = "testCase",
            CancellationToken cancellationToken = default)
        {
            if (file is null || file.Length == 0)
                return BadRequest(ApiResponse<UploadQADocumentResponse>.Fail("No file provided."));

            var result = await _qaService.UploadDocumentAsync(firmwareId, file, type, BaseUrl, CurrentUserId, cancellationToken);
            return Ok(ApiResponse<UploadQADocumentResponse>.Ok(result, "Document uploaded."));
        }

        /// <summary>Removes a document (test case or test result) from the QA session.</summary>
        [HttpDelete("documents/{documentId}")]
        [Authorize(Policy = "CanRunQASession")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveDocument(string firmwareId, string documentId, CancellationToken cancellationToken = default)
        {
            await _qaService.RemoveDocumentAsync(firmwareId, documentId, CurrentUserId, cancellationToken);
            return Ok(ApiResponse.OkNoData("Document removed."));
        }

        /// <summary>Adds a new bug to the QA session bug list.</summary>
        [HttpPost("bugs")]
        [Authorize(Policy = "CanRunQASession")]
        [ProducesResponseType(typeof(ApiResponse<QASessionDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddBug(string firmwareId, [FromBody] AddBugRequest request, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<QASessionDto>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

            var session = await _qaService.AddBugAsync(firmwareId, request, CurrentUserId, cancellationToken);
            return Ok(ApiResponse<QASessionDto>.Ok(session, "Bug added."));
        }

        /// <summary>Updates an existing bug's status, severity, or resolution.</summary>
        [HttpPut("bugs/{bugId}")]
        [Authorize(Policy = "CanRunQASession")]
        [ProducesResponseType(typeof(ApiResponse<QASessionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateBug(string firmwareId, string bugId, [FromBody] UpdateBugRequest request, CancellationToken cancellationToken = default)
        {
            var session = await _qaService.UpdateBugAsync(firmwareId, bugId, request, CurrentUserId, cancellationToken);
            return Ok(ApiResponse<QASessionDto>.Ok(session, "Bug updated."));
        }

        /// <summary>Finalizes the QA session as Complete or Fail.</summary>
        [HttpPost("complete")]
        [Authorize(Policy = "CanRunQASession")]
        [ProducesResponseType(typeof(ApiResponse<QASessionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CompleteSession(string firmwareId, [FromBody] CompleteQARequest request, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<QASessionDto>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

            var session = await _qaService.CompleteSessionAsync(firmwareId, request, CurrentUserId, cancellationToken);
            return Ok(ApiResponse<QASessionDto>.Ok(session, $"QA session completed with status {request.FinalStatus}."));
        }

        /// <summary>Returns the full event log for the QA session, newest first.</summary>
        [HttpGet("log")]
        [Authorize(Policy = "CanViewAudit")]
        [ProducesResponseType(typeof(ApiResponse<List<QAEventLogItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEventLog(string firmwareId, CancellationToken cancellationToken = default)
        {
            var log = await _qaService.GetEventLogAsync(firmwareId, cancellationToken);
            return Ok(ApiResponse<List<QAEventLogItemDto>>.Ok(log, $"{log.Count} event(s) found."));
        }
    }
}
