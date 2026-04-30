using OTA.API.Models.DTOs;
using OTA.API.Models.Entities;
using OTA.API.Repositories.Interfaces;
using OTA.API.Services.Interfaces;

namespace OTA.API.Services
{
    public class ClientService : IClientService
    {
        private readonly IClientRepository _clientRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<ClientService> _logger;

        public ClientService(IClientRepository clientRepository, IEmailService emailService, ILogger<ClientService> logger)
        {
            _clientRepository = clientRepository ?? throw new ArgumentNullException(nameof(clientRepository));
            _emailService     = emailService     ?? throw new ArgumentNullException(nameof(emailService));
            _logger           = logger           ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ClientDto> CreateClientAsync(
            CreateClientRequest request,
            string callerUserId,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Name is required.");
            if (string.IsNullOrWhiteSpace(request.Code)) throw new ArgumentException("Code is required.");

            var code = request.Code.Trim().ToUpperInvariant();

            var existingByCode = await _clientRepository.GetByCodeAsync(code, cancellationToken);
            if (existingByCode != null)
                throw new InvalidOperationException($"A client with code '{code}' already exists.");

            var existingByName = await _clientRepository.GetByNameAsync(request.Name.Trim(), cancellationToken);
            if (existingByName != null)
                throw new InvalidOperationException($"A client with name '{request.Name.Trim()}' already exists.");

            var entity = new ClientEntity
            {
                ClientId        = Guid.NewGuid().ToString(),
                Name            = request.Name.Trim(),
                Code            = code,
                ContactEmail    = request.ContactEmail?.Trim(),
                ContactPhone    = request.ContactPhone?.Trim(),
                Address         = request.Address?.Trim(),
                Notes           = request.Notes?.Trim(),
                IsActive        = true,
                CreatedAt       = DateTime.UtcNow,
                UpdatedAt       = DateTime.UtcNow,
                CreatedByUserId = callerUserId,
            };

            await _clientRepository.InsertAsync(entity, cancellationToken);
            _logger.LogInformation("Client created: {Code} / {Name} by user {UserId}.", entity.Code, entity.Name, callerUserId);
            // Send email — callerUserId used as placeholder; real email not available in this service
            _ = _emailService.SendCrudNotificationAsync(entity.ContactEmail ?? string.Empty, entity.Name, "Created", "Client", entity.Name, CancellationToken.None);
            return MapToDto(entity);
        }

        public async Task<ClientDto> UpdateClientAsync(
            string id,
            UpdateClientRequest request,
            string callerUserId,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var entity = await _clientRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new KeyNotFoundException($"Client '{id}' was not found.");

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var trimmed = request.Name.Trim();
                if (!string.Equals(trimmed, entity.Name, StringComparison.OrdinalIgnoreCase))
                {
                    var conflict = await _clientRepository.GetByNameAsync(trimmed, cancellationToken);
                    if (conflict != null && conflict.Id != entity.Id)
                        throw new InvalidOperationException($"A client with name '{trimmed}' already exists.");
                }
                entity.Name = trimmed;
            }

            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                var code = request.Code.Trim().ToUpperInvariant();
                if (!string.Equals(code, entity.Code, StringComparison.OrdinalIgnoreCase))
                {
                    var conflict = await _clientRepository.GetByCodeAsync(code, cancellationToken);
                    if (conflict != null && conflict.Id != entity.Id)
                        throw new InvalidOperationException($"A client with code '{code}' already exists.");
                }
                entity.Code = code;
            }

            if (request.ContactEmail  != null) entity.ContactEmail  = request.ContactEmail.Trim();
            if (request.ContactPhone  != null) entity.ContactPhone  = request.ContactPhone.Trim();
            if (request.Address       != null) entity.Address       = request.Address.Trim();
            if (request.Notes         != null) entity.Notes         = request.Notes.Trim();
            if (request.IsActive.HasValue)     entity.IsActive      = request.IsActive.Value;

            entity.UpdatedAt = DateTime.UtcNow;
            await _clientRepository.UpdateAsync(id, entity, cancellationToken);
            _logger.LogInformation("Client updated: {Id} by user {UserId}.", id, callerUserId);
            return MapToDto(entity);
        }

        public async Task<ClientDto?> GetClientByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var entity = await _clientRepository.GetByIdAsync(id, cancellationToken);
            return entity is null ? null : MapToDto(entity);
        }

        public async Task<PagedResult<ClientDto>> GetClientsAsync(
            string search,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            page     = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var items      = await _clientRepository.SearchAsync(search, page, pageSize, cancellationToken);
            var totalCount = await _clientRepository.CountAsync(search, cancellationToken);

            return PagedResult<ClientDto>.Create(items.Select(MapToDto).ToList(), page, pageSize, totalCount);
        }

        public async Task DeleteClientAsync(string id, CancellationToken cancellationToken = default)
        {
            var deleted = await _clientRepository.DeleteAsync(id, cancellationToken);
            if (!deleted)
                throw new KeyNotFoundException($"Client '{id}' was not found.");

            _logger.LogInformation("Client deleted: {Id}.", id);
        }

        public Task<string> GetNextCodeAsync(CancellationToken cancellationToken = default)
            => _clientRepository.GetNextCodeAsync("CUSTOM_", cancellationToken);

        private static ClientDto MapToDto(ClientEntity e) => new()
        {
            Id              = e.Id,
            ClientId        = e.ClientId,
            Name            = e.Name,
            Code            = e.Code,
            ContactEmail    = e.ContactEmail,
            ContactPhone    = e.ContactPhone,
            Address         = e.Address,
            Notes           = e.Notes,
            IsActive        = e.IsActive,
            CreatedAt       = e.CreatedAt,
            UpdatedAt       = e.UpdatedAt,
            CreatedByUserId = e.CreatedByUserId,
        };
    }
}
