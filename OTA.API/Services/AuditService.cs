using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OTA.API.Models.DTOs;
using OTA.API.Models.Entities;
using OTA.API.Models.Enums;
using OTA.API.Repositories.Interfaces;
using OTA.API.Services.Interfaces;

namespace OTA.API.Services
{
    /// <summary>
    /// Implements audit logging and export functionality.
    /// LogActionAsync is fire-and-observe: failures are caught and logged but never thrown
    /// so a logging failure cannot break a business operation.
    /// ExportAuditLogsAsync serialises matching entries to UTF-8 CSV bytes.
    /// </summary>
    public class AuditService : IAuditService
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ILogger<AuditService> _logger;

        /// <summary>Initialises a new instance of <see cref="AuditService"/>.</summary>
        public AuditService(IAuditLogRepository auditLogRepository, ILogger<AuditService> logger)
        {
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task LogActionAsync(
            AuditAction action,
            string userId,
            string email,
            UserRole role,
            string entityType,
            string entityId,
            string? oldValue,
            string? newValue,
            string ipAddress,
            string? userAgent = null,
            Dictionary<string, string>? context = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var entry = new AuditLogEntity
                {
                    Action = action,
                    PerformedByUserId = userId,
                    PerformedByEmail = email,
                    PerformedByRole = role.ToString(),
                    EntityType = entityType,
                    EntityId = entityId,
                    OldValue = oldValue,
                    NewValue = newValue,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Context = context ?? new Dictionary<string, string>(),
                    Timestamp = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _auditLogRepository.InsertAsync(entry, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to persist audit log entry for action '{Action}' on entity '{EntityType}/{EntityId}'.",
                    action, entityType, entityId);
            }
        }

        /// <inheritdoc/>
        public async Task<PagedResult<AuditLogDto>> SearchAuditLogsAsync(
            AuditLogFilterRequest filter,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;

            try
            {
                var items = await _auditLogRepository.SearchAsync(filter, page, pageSize, cancellationToken);
                var total = await _auditLogRepository.CountAsync(filter, cancellationToken);

                return new PagedResult<AuditLogDto>
                {
                    Items = items.Select(MapToDto).ToList(),
                    TotalCount = total,
                    Page = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search audit logs.");
                throw new InvalidOperationException("Failed to search audit logs.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<byte[]> ExportAuditLogsAsync(AuditLogFilterRequest filter, CancellationToken cancellationToken = default)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            try
            {
                var items = await _auditLogRepository.ExportAsync(filter, cancellationToken);
                var csv = new StringBuilder();
                csv.AppendLine("Timestamp,Action,PerformedByEmail,PerformedByRole,EntityType,EntityId,IpAddress,OldValue,NewValue");

                foreach (var item in items)
                {
                    csv.AppendLine(string.Join(",",
                        EscapeCsvField(item.Timestamp.ToString("O")),
                        EscapeCsvField(item.Action.ToString()),
                        EscapeCsvField(item.PerformedByEmail),
                        EscapeCsvField(item.PerformedByRole.ToString()),
                        EscapeCsvField(item.EntityType),
                        EscapeCsvField(item.EntityId),
                        EscapeCsvField(item.IpAddress),
                        EscapeCsvField(item.OldValue ?? string.Empty),
                        EscapeCsvField(item.NewValue ?? string.Empty)
                    ));
                }

                return Encoding.UTF8.GetBytes(csv.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export audit logs.");
                throw new InvalidOperationException("Failed to export audit logs.", ex);
            }
        }

        // ── Private helpers ─────────────────────────────────────────────────────────

        private static string EscapeCsvField(string value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static AuditLogDto MapToDto(AuditLogEntity e) => new AuditLogDto
        {
            AuditId = e.AuditId.Length > 0 ? e.AuditId : e.Id,
            Action = e.Action.ToString(),
            PerformedByUserId = e.PerformedByUserId,
            PerformedByEmail = e.PerformedByEmail,
            PerformedByRole = e.PerformedByRole,
            EntityType = e.EntityType,
            EntityId = e.EntityId,
            OldValue = e.OldValue,
            NewValue = e.NewValue,
            IpAddress = e.IpAddress,
            UserAgent = e.UserAgent,
            Timestamp = e.Timestamp
        };
    }
}
