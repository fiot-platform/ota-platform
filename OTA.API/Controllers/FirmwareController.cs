using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OTA.API.Helpers;
using OTA.API.Models.DTOs;
using OTA.API.Models.Enums;
using OTA.API.Models.Settings;
using OTA.API.Services.Interfaces;

namespace OTA.API.Controllers
{
    /// <summary>
    /// Firmware lifecycle management endpoints including QA verification, approval, rejection,
    /// channel assignment, and Gitea sync.
    /// Workflow: Draft -> PendingQA -> QAVerified -> PendingApproval -> Approved | Rejected.
    /// </summary>
    [ApiController]
    [Route("api/firmware")]
    [Authorize]
    [Produces("application/json")]
    public class FirmwareController : ControllerBase
    {
        private readonly IFirmwareService _firmwareService;
        private readonly IUserService _userService;
        private readonly GiteaSettings _giteaSettings;
        private readonly ILogger<FirmwareController> _logger;

        public FirmwareController(
            IFirmwareService firmwareService,
            IUserService userService,
            IOptions<GiteaSettings> giteaSettings,
            ILogger<FirmwareController> logger)
        {
            _firmwareService = firmwareService ?? throw new ArgumentNullException(nameof(firmwareService));
            _userService     = userService ?? throw new ArgumentNullException(nameof(userService));
            _giteaSettings   = giteaSettings?.Value ?? throw new ArgumentNullException(nameof(giteaSettings));
            _logger          = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private string CurrentUserId =>
            User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        private string CurrentEmail =>
            User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? string.Empty;

        private string CurrentName =>
            User.FindFirstValue("fullName") ?? User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        private string ClientIp =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

        private async Task<List<string>?> GetProjectScopeAsync(CancellationToken cancellationToken = default)
        {
            // SuperAdmin and PlatformAdmin see everything — no filtering.
            var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? string.Empty;
            if (role is "SuperAdmin" or "PlatformAdmin") return null;

            // Fetch live project scope from the database so changes take effect without re-login.
            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return new List<string>();

            var user = await _userService.GetUserByIdAsync(userId, cancellationToken);
            return user?.ProjectScope ?? new List<string>();
        }

        /// <summary>Returns a paginated list of firmware records. All authenticated roles.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<FirmwareDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFirmwareList(
            [FromQuery] string? search = null,
            [FromQuery] string? filter = null,
            [FromQuery] string? status = null,
            [FromQuery] string? channel = null,
            [FromQuery] string? projectId = null,
            [FromQuery] string? repositoryId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            CancellationToken cancellationToken = default)
        {
            // Accept either 'search' or legacy 'filter' param
            var searchTerm = search ?? filter ?? string.Empty;
            var allowedProjectIds = await GetProjectScopeAsync(cancellationToken);

            // Roles that are not part of the approval workflow only see Approved firmware.
            // SuperAdmin, PlatformAdmin, ReleaseManager, and QA can see all statuses.
            var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? string.Empty;
            if (role is not ("SuperAdmin" or "PlatformAdmin" or "ReleaseManager" or "QA"))
                status = FirmwareStatus.Approved.ToString();

            var result = await _firmwareService.GetFirmwareListAsync(searchTerm, status, channel, projectId, repositoryId, page, pageSize, allowedProjectIds, cancellationToken);
            var pagination = PaginationInfo.Create(page, pageSize, result.TotalCount);
            return Ok(ApiResponse<List<FirmwareDto>>.Ok(result.Items, "Firmware list retrieved successfully.", pagination));
        }

        /// <summary>Returns a single firmware record by its identifier.</summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<FirmwareDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFirmwareById(string id, CancellationToken cancellationToken = default)
        {
            var firmware = await _firmwareService.GetFirmwareByIdAsync(id, cancellationToken);
            if (firmware == null)
                return NotFound(ApiResponse<FirmwareDto>.Fail($"Firmware '{id}' was not found."));

            // Prefer the denormalised name stored on creation; fall back to a live user lookup for
            // older records that were created before this field was introduced.
            if (string.IsNullOrWhiteSpace(firmware.CreatedByName) && !string.IsNullOrWhiteSpace(firmware.CreatedByUserId))
            {
                try
                {
                    var creator = await _userService.GetUserByIdAsync(firmware.CreatedByUserId, cancellationToken);
                    if (creator != null)
                        firmware.CreatedByName = !string.IsNullOrWhiteSpace(creator.Name)
                            ? creator.Name
                            : creator.Email;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not enrich firmware creator name for userId={UserId}", firmware.CreatedByUserId);
                }
            }

            // Enrich QA verifier name.
            if (!string.IsNullOrWhiteSpace(firmware.QaVerifiedByUserId))
            {
                try
                {
                    var qaUser = await _userService.GetUserByIdAsync(firmware.QaVerifiedByUserId, cancellationToken);
                    if (qaUser != null)
                        firmware.QaVerifiedByName = !string.IsNullOrWhiteSpace(qaUser.Name)
                            ? qaUser.Name
                            : qaUser.Email;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not enrich QA verifier name for userId={UserId}", firmware.QaVerifiedByUserId);
                }
            }

            // Enrich approver name.
            if (!string.IsNullOrWhiteSpace(firmware.ApprovedByUserId))
            {
                try
                {
                    var approver = await _userService.GetUserByIdAsync(firmware.ApprovedByUserId, cancellationToken);
                    if (approver != null)
                        firmware.ApprovedByName = !string.IsNullOrWhiteSpace(approver.Name)
                            ? approver.Name
                            : approver.Email;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not enrich approver name for userId={UserId}", firmware.ApprovedByUserId);
                }
            }

            // Enrich rejector name.
            if (!string.IsNullOrWhiteSpace(firmware.RejectedByUserId))
            {
                try
                {
                    var rejector = await _userService.GetUserByIdAsync(firmware.RejectedByUserId, cancellationToken);
                    if (rejector != null)
                        firmware.RejectedByName = !string.IsNullOrWhiteSpace(rejector.Name)
                            ? rejector.Name
                            : rejector.Email;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not enrich rejector name for userId={UserId}", firmware.RejectedByUserId);
                }
            }

            return Ok(ApiResponse<FirmwareDto>.Ok(firmware));
        }

        /// <summary>Creates a new firmware record in Draft status. ReleaseManager, PlatformAdmin, SuperAdmin.</summary>
        [HttpPost]
        [Authorize(Policy = "CanSyncFirmware")]
        [ProducesResponseType(typeof(ApiResponse<FirmwareDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateFirmware(
            [FromBody] CreateFirmwareRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<FirmwareDto>.Fail("Validation failed.", errors));
            }

            var created = await _firmwareService.CreateFirmwareAsync(request, CurrentUserId, CurrentName, CurrentEmail, ClientIp, cancellationToken);
            return CreatedAtAction(nameof(GetFirmwareById), new { id = created.FirmwareId },
                ApiResponse<FirmwareDto>.Ok(created, "Firmware created successfully."));
        }

        /// <summary>Updates a firmware record's metadata. ReleaseManager, PlatformAdmin, SuperAdmin.</summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "CanSyncFirmware")]
        [ProducesResponseType(typeof(ApiResponse<FirmwareDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateFirmware(
            string id,
            [FromBody] UpdateFirmwareRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<FirmwareDto>.Fail("Validation failed.", errors));
            }

            var updated = await _firmwareService.UpdateFirmwareAsync(id, request, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
            return Ok(ApiResponse<FirmwareDto>.Ok(updated, "Firmware updated successfully."));
        }

        /// <summary>
        /// Proxies the firmware binary download — streams the file from Gitea to the authenticated
        /// caller so the real Gitea URL is never exposed to the client.
        /// </summary>
        [HttpGet("{id}/download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadFirmware(string id, CancellationToken cancellationToken = default)
        {
            var firmware = await _firmwareService.GetFirmwareByIdAsync(id, cancellationToken);
            if (firmware == null)
                return NotFound(ApiResponse<object>.Fail("Firmware not found."));

            if (string.IsNullOrWhiteSpace(firmware.DownloadUrl))
                return NotFound(ApiResponse<object>.Fail("No download URL is available for this firmware."));

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _giteaSettings.AdminToken);

            var response = await httpClient.GetAsync(firmware.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gitea returned {Code} when downloading firmware '{Id}' from '{Url}'.",
                    (int)response.StatusCode, id, firmware.DownloadUrl);
                return NotFound(ApiResponse<object>.Fail($"File could not be retrieved from storage (HTTP {(int)response.StatusCode})."));
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            var fileName = !string.IsNullOrWhiteSpace(firmware.FileName) ? firmware.FileName : $"firmware-{firmware.Version}.bin";
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            Response.Headers["Content-Disposition"] = $"attachment; filename=\"{fileName}\"";
            return File(stream, contentType, fileName);
        }

        /// <summary>Submits QA verification for a firmware record. QA team and Release Manager.</summary>
        [HttpPost("{id}/qa-verify")]
        [Authorize(Policy = "CanRunQASession")]
        [ProducesResponseType(typeof(ApiResponse<FirmwareDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> QAVerifyFirmware(
            string id,
            [FromBody] QAVerifyFirmwareRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<FirmwareDto>.Fail("Validation failed.", errors));
            }

            var result = await _firmwareService.QAVerifyFirmwareAsync(id, CurrentUserId, request.QaRemarks ?? string.Empty, ClientIp, cancellationToken);
            return Ok(ApiResponse<FirmwareDto>.Ok(result, "Firmware QA verification recorded."));
        }

        /// <summary>Approves a firmware record. SuperAdmin and PlatformAdmin only.</summary>
        [HttpPost("{id}/approve")]
        [Authorize(Policy = "CanApproveFirmware")]
        [ProducesResponseType(typeof(ApiResponse<FirmwareDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApproveFirmware(
            string id,
            [FromBody] ApproveFirmwareRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<FirmwareDto>.Fail("Validation failed.", errors));
            }

            var result = await _firmwareService.ApproveFirmwareAsync(id, CurrentUserId, request.ApprovalNotes ?? string.Empty, ClientIp, cancellationToken);
            return Ok(ApiResponse<FirmwareDto>.Ok(result, "Firmware approved successfully."));
        }

        /// <summary>Rejects a firmware record. SuperAdmin and PlatformAdmin only.</summary>
        [HttpPost("{id}/reject")]
        [Authorize(Policy = "CanApproveFirmware")]
        [ProducesResponseType(typeof(ApiResponse<FirmwareDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RejectFirmware(
            string id,
            [FromBody] RejectFirmwareRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<FirmwareDto>.Fail("Validation failed.", errors));
            }

            var result = await _firmwareService.RejectFirmwareAsync(id, CurrentUserId, request.RejectionReason ?? string.Empty, ClientIp, cancellationToken);
            return Ok(ApiResponse<FirmwareDto>.Ok(result, "Firmware rejected."));
        }

        /// <summary>Assigns the firmware to a release channel. ReleaseManager, PlatformAdmin, SuperAdmin.</summary>
        [HttpPost("{id}/assign-channel")]
        [Authorize(Policy = "CanSyncFirmware")]
        [ProducesResponseType(typeof(ApiResponse<FirmwareDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignChannel(
            string id,
            [FromBody] AssignChannelRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<FirmwareDto>.Fail("Validation failed.", errors));
            }

            var result = await _firmwareService.AssignChannelAsync(id, request.Channel, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
            return Ok(ApiResponse<FirmwareDto>.Ok(result, "Channel assigned successfully."));
        }

        /// <summary>Permanently deletes a firmware record. SuperAdmin only. Approved firmware cannot be deleted.</summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "SuperAdmin")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteFirmware(string id, CancellationToken cancellationToken = default)
        {
            await _firmwareService.DeleteFirmwareAsync(id, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
            return Ok(ApiResponse.OkNoData("Firmware deleted successfully."));
        }

        /// <summary>Deprecates an approved firmware. SuperAdmin and PlatformAdmin only.</summary>
        [HttpPost("{id}/deprecate")]
        [Authorize(Policy = "CanManageProjects")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeprecateFirmware(string id, CancellationToken cancellationToken = default)
        {
            await _firmwareService.DeprecateFirmwareAsync(id, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
            return Ok(ApiResponse.OkNoData("Firmware deprecated successfully."));
        }

        /// <summary>
        /// Uploads a firmware binary file to the server's local storage.
        /// Returns the stored filename, size, SHA-256 checksum, and a download URL that can be
        /// pasted into the CreateFirmware form fields.
        /// </summary>
        [HttpPost("upload-file")]
        [Authorize(Policy = "CanSyncFirmware")]
        [RequestSizeLimit(536_870_912)] // 512 MB hard cap
        [RequestFormLimits(MultipartBodyLengthLimit = 536_870_912)]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ApiResponse<UploadFirmwareFileResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadFirmwareFile(
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
                return BadRequest(ApiResponse<UploadFirmwareFileResponse>.Fail("No file provided."));

            // Sanitise extension and generate a collision-free stored name
            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(ext)) ext = ".bin";
            var storedName = $"{Guid.NewGuid():N}{ext}";

            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "firmware-uploads");
            Directory.CreateDirectory(uploadDir);
            var filePath = Path.Combine(uploadDir, storedName);

            // Stream to disk while computing SHA-256 in one pass
            string sha256Hex;
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                await using var fs = System.IO.File.Create(filePath);
                await using var src = file.OpenReadStream();
                var buf = new byte[81_920];
                int read;
                while ((read = await src.ReadAsync(buf, cancellationToken)) > 0)
                {
                    sha256.TransformBlock(buf, 0, read, null, 0);
                    await fs.WriteAsync(buf.AsMemory(0, read), cancellationToken);
                }
                sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                sha256Hex = Convert.ToHexString(sha256.Hash!).ToLowerInvariant();
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var downloadUrl = $"{baseUrl}/firmware-uploads/{storedName}";

            _logger.LogInformation("Firmware file uploaded by {User}: {OrigName} → {StoredName} ({Bytes} bytes, sha256={Hash})",
                CurrentUserId, file.FileName, storedName, file.Length, sha256Hex);

            return Ok(ApiResponse<UploadFirmwareFileResponse>.Ok(new UploadFirmwareFileResponse
            {
                FileName      = file.FileName,
                StoredFileName = storedName,
                FileSizeBytes  = file.Length,
                FileSha256     = sha256Hex,
                DownloadUrl    = downloadUrl,
            }, "File uploaded successfully."));
        }

        /// <summary>Triggers a firmware sync from Gitea for the given repository. DevOps, PlatformAdmin, SuperAdmin.</summary>
        [HttpPost("sync/{repositoryId}")]
        [Authorize(Policy = "CanSyncRepository")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SyncFirmwareFromGitea(string repositoryId, CancellationToken cancellationToken = default)
        {
            var count = await _firmwareService.SyncFirmwareFromGiteaAsync(repositoryId, cancellationToken);
            return Ok(ApiResponse<object>.Ok(new { NewRecords = count }, $"Sync complete. {count} new firmware record(s) created."));
        }
    }
}
