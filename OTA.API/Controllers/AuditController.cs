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
    /// Audit log query and export endpoints. All operations require the CanViewAudit policy.
    /// SuperAdmin, PlatformAdmin, and Auditor roles are granted this policy by default.
    /// </summary>
    [ApiController]
    [Route("api/audit")]
    [Authorize(Policy = "CanViewAudit")]
    [Produces("application/json")]
    public class AuditController : ControllerBase
    {
        private readonly IAuditService _auditService;
        private readonly ILogger<AuditController> _logger;

        public AuditController(IAuditService auditService, ILogger<AuditController> logger)
        {
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _logger       = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Searches audit log entries using structured filter criteria.
        /// Supports filtering by date range, action type, user, entity type and entity ID.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<AuditLogDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchAuditLogs(
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo   = null,
            [FromQuery] string?   action   = null,
            [FromQuery] string?   userId   = null,
            [FromQuery] string?   entityType = null,
            [FromQuery] string?   entityId   = null,
            [FromQuery] string?   role       = null,
            [FromQuery] int       page       = 1,
            [FromQuery] int       pageSize   = 25,
            CancellationToken cancellationToken = default)
        {
            AuditAction? parsedAction = null;
            if (!string.IsNullOrWhiteSpace(action) &&
                Enum.TryParse<AuditAction>(action, ignoreCase: true, out var auditAction))
                parsedAction = auditAction;

            var filter = new AuditLogFilterRequest
            {
                DateFrom   = dateFrom,
                DateTo     = dateTo,
                Action     = parsedAction,
                UserId     = userId,
                EntityType = entityType,
                EntityId   = entityId,
                Role       = role,
                Page       = page,
                PageSize   = pageSize
            };

            var result = await _auditService.SearchAuditLogsAsync(filter, page, pageSize, cancellationToken);
            var pagination = PaginationInfo.Create(page, pageSize, result.TotalCount);
            return Ok(ApiResponse<List<AuditLogDto>>.Ok(result.Items, "Audit logs retrieved successfully.", pagination));
        }

        /// <summary>
        /// Exports audit log entries matching the filter as a UTF-8 encoded CSV file.
        /// The caller receives an octet-stream response suitable for download.
        /// </summary>
        [HttpGet("export")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ExportAuditLogs(
            [FromQuery] DateTime? dateFrom   = null,
            [FromQuery] DateTime? dateTo     = null,
            [FromQuery] string?   action     = null,
            [FromQuery] string?   userId     = null,
            [FromQuery] string?   entityType = null,
            [FromQuery] string?   entityId   = null,
            [FromQuery] string?   role       = null,
            CancellationToken cancellationToken = default)
        {
            AuditAction? parsedActionExport = null;
            if (!string.IsNullOrWhiteSpace(action) &&
                Enum.TryParse<AuditAction>(action, ignoreCase: true, out var exportAction))
                parsedActionExport = exportAction;

            var filter = new AuditLogFilterRequest
            {
                DateFrom   = dateFrom,
                DateTo     = dateTo,
                Action     = parsedActionExport,
                UserId     = userId,
                EntityType = entityType,
                EntityId   = entityId,
                Role       = role
            };

            var csvBytes = await _auditService.ExportAuditLogsAsync(filter, cancellationToken);
            var fileName  = $"audit-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";

            _logger.LogInformation("Audit log export requested. Filter={Filter}.", System.Text.Json.JsonSerializer.Serialize(filter));

            return File(csvBytes, "text/csv", fileName);
        }
    }
}
