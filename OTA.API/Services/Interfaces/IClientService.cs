using OTA.API.Models.DTOs;

namespace OTA.API.Services.Interfaces
{
    /// <summary>
    /// Service interface for client (customer organisation) CRUD operations.
    /// </summary>
    public interface IClientService
    {
        Task<ClientDto> CreateClientAsync(CreateClientRequest request, string callerUserId, CancellationToken cancellationToken = default);

        Task<ClientDto> UpdateClientAsync(string id, UpdateClientRequest request, string callerUserId, CancellationToken cancellationToken = default);

        Task<ClientDto?> GetClientByIdAsync(string id, CancellationToken cancellationToken = default);

        Task<PagedResult<ClientDto>> GetClientsAsync(string search, int page, int pageSize, CancellationToken cancellationToken = default);

        Task DeleteClientAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>Returns the next available sequential client code (e.g. "CUSTOM_00003").</summary>
        Task<string> GetNextCodeAsync(CancellationToken cancellationToken = default);
    }
}
