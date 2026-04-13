using System.Threading;
using System.Threading.Tasks;
using OTA.API.Models.DTOs;
using OTA.API.Models.Enums;

namespace OTA.API.Services.Interfaces
{
    /// <summary>
    /// Service interface for audit logging and export operations.
    /// </summary>
    public interface IAuditService
    {
        /// <summary>
        /// Logs a single audit action asynchronously. Non-blocking; failures are swallowed and logged to the application logger.
        /// </summary>
        /// <param name="action">The type of action being logged.</param>
        /// <param name="userId">The identifier of the user who performed the action.</param>
        /// <param name="email">The email address of the user for display purposes.</param>
        /// <param name="role">The role of the user at the time of the action.</param>
        /// <param name="entityType">The entity type affected by the action (e.g. "Firmware", "Device").</param>
        /// <param name="entityId">The identifier of the entity affected.</param>
        /// <param name="oldValue">JSON-serialised previous state of the entity (null for creates).</param>
        /// <param name="newValue">JSON-serialised new state of the entity (null for deletes).</param>
        /// <param name="ipAddress">The IP address of the client performing the action.</param>
        /// <param name="userAgent">The User-Agent header string of the client (optional).</param>
        /// <param name="context">Additional contextual metadata as key-value pairs (optional).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task LogActionAsync(
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
            System.Collections.Generic.Dictionary<string, string>? context = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches audit log entries using a structured filter with pagination.
        /// </summary>
        /// <param name="filter">The structured filter object containing date range, action, user, and entity constraints.</param>
        /// <param name="page">One-based page number.</param>
        /// <param name="pageSize">Number of results per page.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A paged result containing audit log DTOs and total count.</returns>
        Task<PagedResult<AuditLogDto>> SearchAuditLogsAsync(AuditLogFilterRequest filter, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Exports all audit log entries matching the filter as a UTF-8 encoded CSV byte array.
        /// </summary>
        /// <param name="filter">The structured filter for the export dataset.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Byte array containing the UTF-8 CSV content.</returns>
        Task<byte[]> ExportAuditLogsAsync(AuditLogFilterRequest filter, CancellationToken cancellationToken = default);
    }
}
