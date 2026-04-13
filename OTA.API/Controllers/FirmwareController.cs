using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OTA.API.Helpers;
using OTA.API.Models.DTOs;
using OTA.API.Models.Enums;
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
        private readonly ILogger<FirmwareController> _logger;

        public FirmwareController(IFirmwareService firmwareService, ILogger<FirmwareController> logger)
        {
            _firmwareService = firmwareService ?? throw new ArgumentNullException(nameof(firmwareService));
            _logger          = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private string CurrentUserId =>
            User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        private string CurrentEmail =>
            User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? string.Empty;

        private string ClientIp =>
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

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
            var result = await _firmwareService.GetFirmwareListAsync(searchTerm, status, channel, projectId, repositoryId, page, pageSize, cancellationToken);
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

            var created = await _firmwareService.CreateFirmwareAsync(request, CurrentUserId, CurrentEmail, ClientIp, cancellationToken);
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

        /// <summary>Submits QA verification for a firmware record. QA team only.</summary>
        [HttpPost("{id}/qa-verify")]
        [Authorize(Policy = "CanApproveFirmware")]
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
