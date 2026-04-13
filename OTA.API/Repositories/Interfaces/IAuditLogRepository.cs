using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OTA.API.Models.DTOs;
using OTA.API.Models.Entities;

namespace OTA.API.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for AuditLogEntity providing audit log query and export operations.
    /// </summary>
    public interface IAuditLogRepository : IBaseRepository<AuditLogEntity>
    {
        /// <summary>
        /// Searches audit log entries using a structured filter object with pagination.
        /// </summary>
        /// <param name="filter">The structured filter containing date range, action, user, and entity constraints.</param>
        /// <param name="page">One-based page number.</param>
        /// <param name="pageSize">Number of results per page.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Paged list of matching audit log entries.</returns>
        Task<List<AuditLogEntity>> SearchAsync(AuditLogFilterRequest filter, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all audit log entries matching the filter for export (no pagination cap).
        /// </summary>
        /// <param name="filter">The structured filter for the export dataset.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>All matching audit log entries.</returns>
        Task<List<AuditLogEntity>> ExportAsync(AuditLogFilterRequest filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Counts audit log entries matching the specified filter.
        /// </summary>
        /// <param name="filter">The structured filter to count against.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Total count of matching audit log entries.</returns>
        Task<long> CountAsync(AuditLogFilterRequest filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves audit log entries for a specific entity type and entity identifier.
        /// </summary>
        /// <param name="entityType">The type name of the entity (e.g., "Firmware", "Device").</param>
        /// <param name="entityId">The unique identifier of the entity.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Chronological list of audit log entries for the entity.</returns>
        Task<List<AuditLogEntity>> GetByEntityAsync(string entityType, string entityId, CancellationToken cancellationToken = default);
    }
}
