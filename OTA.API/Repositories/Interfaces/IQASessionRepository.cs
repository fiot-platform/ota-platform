using OTA.API.Models.Entities;
using OTA.API.Models.Enums;

namespace OTA.API.Repositories.Interfaces
{
    /// <summary>Repository interface for QASessionEntity with targeted array-mutation operations.</summary>
    public interface IQASessionRepository
    {
        Task<QASessionEntity?> GetByFirmwareIdAsync(string firmwareId, CancellationToken cancellationToken = default);
        Task<Dictionary<string, string>> GetStatusByFirmwareIdsAsync(IEnumerable<string> firmwareIds, CancellationToken cancellationToken = default);
        Task InsertAsync(QASessionEntity entity, CancellationToken cancellationToken = default);
        Task UpdateStatusAsync(string id, QASessionStatus status, string? remarks, CancellationToken cancellationToken = default);
        Task PushTestCaseDocumentAsync(string id, QADocumentItem doc, CancellationToken cancellationToken = default);
        Task PushTestResultDocumentAsync(string id, QADocumentItem doc, CancellationToken cancellationToken = default);
        Task PullDocumentAsync(string id, string documentId, CancellationToken cancellationToken = default);
        Task PushBugAsync(string id, QABugItem bug, CancellationToken cancellationToken = default);
        Task UpdateBugAsync(string id, string bugId, QABugItem updated, CancellationToken cancellationToken = default);
        Task PushEventLogAsync(string id, QAEventLogItem entry, CancellationToken cancellationToken = default);
        Task SetCompletedAsync(string id, QASessionStatus finalStatus, string? remarks, CancellationToken cancellationToken = default);
    }
}
