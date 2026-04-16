using Microsoft.AspNetCore.Http;
using OTA.API.Models.DTOs;

namespace OTA.API.Services.Interfaces
{
    /// <summary>Service interface for managing QA sessions tied to firmware versions.</summary>
    public interface IQAService
    {
        Task<QASessionDto?> GetSessionAsync(string firmwareId, CancellationToken cancellationToken = default);

        Task<QASessionDto> StartSessionAsync(string firmwareId, string userId, string? userName = null, CancellationToken cancellationToken = default);

        Task<QASessionDto> UpdateStatusAsync(string firmwareId, UpdateQAStatusRequest request, string userId, CancellationToken cancellationToken = default);

        Task<UploadQADocumentResponse> UploadDocumentAsync(string firmwareId, IFormFile file, string docType, string baseUrl, string userId, CancellationToken cancellationToken = default);

        Task<bool> RemoveDocumentAsync(string firmwareId, string documentId, string userId, CancellationToken cancellationToken = default);

        Task<QASessionDto> AddBugAsync(string firmwareId, AddBugRequest request, string userId, CancellationToken cancellationToken = default);

        Task<QASessionDto> UpdateBugAsync(string firmwareId, string bugId, UpdateBugRequest request, string userId, CancellationToken cancellationToken = default);

        Task<QASessionDto> CompleteSessionAsync(string firmwareId, CompleteQARequest request, string userId, CancellationToken cancellationToken = default);

        Task<List<QAEventLogItemDto>> GetEventLogAsync(string firmwareId, CancellationToken cancellationToken = default);
    }
}
