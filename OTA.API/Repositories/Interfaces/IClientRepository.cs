using OTA.API.Models.Entities;

namespace OTA.API.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for ClientEntity providing client-specific query operations.
    /// </summary>
    public interface IClientRepository : IBaseRepository<ClientEntity>
    {
        /// <summary>Retrieves a client by its unique short code (case-insensitive).</summary>
        Task<ClientEntity?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>Retrieves a client by its exact display name (case-insensitive).</summary>
        Task<ClientEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>Searches clients by name or code with pagination.</summary>
        Task<List<ClientEntity>> SearchAsync(string filter, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>Counts clients matching the given filter text.</summary>
        Task<long> CountAsync(string filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the next available sequential code with the given prefix
        /// (e.g. "CUSTOM_00003" when "CUSTOM_00001" and "CUSTOM_00002" already exist).
        /// </summary>
        Task<string> GetNextCodeAsync(string prefix = "CUSTOM_", CancellationToken cancellationToken = default);
    }
}
