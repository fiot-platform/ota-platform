using System.Text.Json;
using OTA.API.Models.DTOs;
using OTA.API.Models.Entities;
using OTA.API.Models.Enums;
using OTA.API.Repositories.Interfaces;
using OTA.API.Services.Interfaces;

namespace OTA.API.Services
{
    /// <summary>
    /// Processes incoming Gitea webhook events, persists them to the repository event store,
    /// and dispatches firmware syncs or metadata logs based on event type.
    /// Supports reprocessing of previously failed events.
    /// </summary>
    public class GiteaWebhookService : IGiteaWebhookService
    {
        private readonly IRepositoryEventRepository _eventRepository;
        private readonly IRepositoryMasterRepository _repoRepository;
        private readonly IFirmwareService _firmwareService;
        private readonly IGiteaApiService _giteaApiService;
        private readonly ILogger<GiteaWebhookService> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>Initialises a new instance of <see cref="GiteaWebhookService"/>.</summary>
        public GiteaWebhookService(
            IRepositoryEventRepository eventRepository,
            IRepositoryMasterRepository repoRepository,
            IFirmwareService firmwareService,
            IGiteaApiService giteaApiService,
            ILogger<GiteaWebhookService> logger)
        {
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            _repoRepository  = repoRepository  ?? throw new ArgumentNullException(nameof(repoRepository));
            _firmwareService = firmwareService  ?? throw new ArgumentNullException(nameof(firmwareService));
            _giteaApiService = giteaApiService  ?? throw new ArgumentNullException(nameof(giteaApiService));
            _logger          = logger           ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<bool> ProcessWebhookAsync(
            string payload,
            string eventType,
            string deliveryId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(payload))    throw new ArgumentException("Payload is required.",    nameof(payload));
            if (string.IsNullOrWhiteSpace(eventType))  throw new ArgumentException("EventType is required.",  nameof(eventType));
            if (string.IsNullOrWhiteSpace(deliveryId)) throw new ArgumentException("DeliveryId is required.", nameof(deliveryId));

            var giteaEventType = ParseEventType(eventType);

            long    giteaRepoId   = 0;
            string? giteaOwner    = null;
            string? giteaRepoName = null;

            try
            {
                using var doc = JsonDocument.Parse(payload);
                if (doc.RootElement.TryGetProperty("repository", out var repoEl))
                {
                    if (repoEl.TryGetProperty("id", out var idEl))
                        giteaRepoId = idEl.GetInt64();
                    if (repoEl.TryGetProperty("owner", out var ownerEl) &&
                        ownerEl.TryGetProperty("login", out var loginEl))
                        giteaOwner = loginEl.GetString();
                    if (repoEl.TryGetProperty("name", out var nameEl))
                        giteaRepoName = nameEl.GetString();
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse webhook payload for delivery '{DeliveryId}'.", deliveryId);
            }

            var eventEntity = new RepositoryEventEntity
            {
                EventId        = Guid.NewGuid().ToString(),
                DeliveryId     = deliveryId,
                GiteaRepoId    = giteaRepoId,
                GiteaEventType = giteaEventType,
                RawPayload     = payload,
                Status         = WebhookEventStatus.Received,
                ReceivedAt     = DateTime.UtcNow,
                CreatedAt      = DateTime.UtcNow,
                RetryCount     = 0
            };

            await _eventRepository.InsertAsync(eventEntity, cancellationToken);
            _logger.LogInformation("Webhook event '{DeliveryId}' ({EventType}) saved.", deliveryId, eventType);

            try
            {
                var success = await DispatchEventAsync(
                    eventEntity, giteaOwner, giteaRepoName, giteaRepoId, cancellationToken);
                eventEntity.Status      = success ? WebhookEventStatus.Processed : WebhookEventStatus.Skipped;
                eventEntity.ProcessedAt = DateTime.UtcNow;
                await _eventRepository.UpdateAsync(eventEntity.Id, eventEntity, cancellationToken);
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process webhook event '{DeliveryId}'.", deliveryId);
                eventEntity.Status          = WebhookEventStatus.Failed;
                eventEntity.ProcessingError = ex.Message;
                await _eventRepository.UpdateAsync(eventEntity.Id, eventEntity, cancellationToken);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task ReprocessEventAsync(string eventId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(eventId))
                throw new ArgumentException("EventId is required.", nameof(eventId));

            var eventEntity = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
                ?? throw new KeyNotFoundException($"Repository event '{eventId}' not found.");

            if (eventEntity.Status != WebhookEventStatus.Failed)
                throw new InvalidOperationException(
                    $"Only Failed events can be reprocessed. Current status: {eventEntity.Status}.");

            _logger.LogInformation("Reprocessing webhook event '{EventId}' (retry #{RetryCount}).",
                eventId, eventEntity.RetryCount + 1);

            string? giteaOwner    = null;
            string? giteaRepoName = null;
            try
            {
                using var doc = JsonDocument.Parse(eventEntity.RawPayload);
                if (doc.RootElement.TryGetProperty("repository", out var repoEl))
                {
                    if (repoEl.TryGetProperty("owner", out var ownerEl) &&
                        ownerEl.TryGetProperty("login", out var loginEl))
                        giteaOwner = loginEl.GetString();
                    if (repoEl.TryGetProperty("name", out var nameEl))
                        giteaRepoName = nameEl.GetString();
                }
            }
            catch { /* best-effort */ }

            try
            {
                var success = await DispatchEventAsync(
                    eventEntity, giteaOwner, giteaRepoName, eventEntity.GiteaRepoId, cancellationToken);
                eventEntity.Status          = success ? WebhookEventStatus.Processed : WebhookEventStatus.Skipped;
                eventEntity.ProcessedAt     = DateTime.UtcNow;
                eventEntity.ProcessingError = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reprocess of event '{EventId}' failed.", eventId);
                eventEntity.Status          = WebhookEventStatus.Failed;
                eventEntity.ProcessingError = ex.Message;
            }

            eventEntity.RetryCount++;
            await _eventRepository.UpdateAsync(eventId, eventEntity, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<List<RepositoryEventDto>> GetRecentEventsAsync(
            RepositoryEventFilterRequest? filter,
            CancellationToken cancellationToken = default)
        {
            List<RepositoryEventEntity> events;

            if (!string.IsNullOrWhiteSpace(filter?.GiteaRepoId))
                events = await _eventRepository.GetByGiteaRepoIdAsync(filter.GiteaRepoId, cancellationToken);
            else
                events = await _eventRepository.GetRecentEventsAsync(filter?.Limit ?? 20, cancellationToken);

            return events.Select(MapToDto).ToList();
        }

        // ── Private helpers ─────────────────────────────────────────────────────────

        private async Task<bool> DispatchEventAsync(
            RepositoryEventEntity eventEntity,
            string? giteaOwner,
            string? giteaRepoName,
            long giteaRepoId,
            CancellationToken cancellationToken)
        {
            switch (eventEntity.GiteaEventType)
            {
                case GiteaEventType.Release:
                {
                    if (giteaRepoId == 0) return false;

                    var repo = await _repoRepository.GetByGiteaRepoIdAsync(
                        giteaRepoId.ToString(), cancellationToken);
                    if (repo == null || !repo.IsActive)
                    {
                        _logger.LogWarning(
                            "Release event for unknown or inactive Gitea repo '{RepoId}'.", giteaRepoId);
                        return false;
                    }

                    var count = await _firmwareService.SyncFirmwareFromGiteaAsync(repo.Id, cancellationToken);
                    _logger.LogInformation(
                        "Release webhook triggered firmware sync for repo '{RepoId}': {Count} new record(s).",
                        repo.Id, count);
                    return true;
                }

                case GiteaEventType.Push:
                {
                    _logger.LogInformation(
                        "Push event received for repo '{RepoId}'. Syncing metadata.", giteaRepoId);

                    if (giteaRepoId != 0)
                    {
                        var repo = await _repoRepository.GetByGiteaRepoIdAsync(
                            giteaRepoId.ToString(), cancellationToken);
                        if (repo != null)
                            await _giteaApiService.SyncRepositoryMetadataAsync(repo.Id, cancellationToken);
                    }
                    return true;
                }

                default:
                    _logger.LogInformation(
                        "Webhook event type '{Type}' not handled; skipping.", eventEntity.GiteaEventType);
                    return false;
            }
        }

        private static GiteaEventType ParseEventType(string eventType) =>
            eventType.ToLowerInvariant() switch
            {
                "release"      => GiteaEventType.Release,
                "push"         => GiteaEventType.Push,
                "create"       => GiteaEventType.Create,
                "delete"       => GiteaEventType.Delete,
                "pull_request" => GiteaEventType.PullRequest,
                "issues"       => GiteaEventType.Issues,
                _              => GiteaEventType.Unknown
            };

        private static RepositoryEventDto MapToDto(RepositoryEventEntity e) => new()
        {
            Id           = e.Id,
            GiteaRepoId  = e.GiteaRepoId.ToString(),
            DeliveryId   = e.DeliveryId,
            EventType    = e.GiteaEventType,
            Status       = e.Status,
            ReceivedAt   = e.ReceivedAt,
            ProcessedAt  = e.ProcessedAt,
            RetryCount   = e.RetryCount,
            ErrorMessage = e.ProcessingError
        };
    }
}
