using OTA.API.Models.Enums;
using OTA.API.Repositories.Interfaces;
using OTA.API.Services.Interfaces;

namespace OTA.API.BackgroundJobs
{
    /// <summary>
    /// Background service that polls for failed Gitea webhook events every 5 minutes and attempts
    /// to reprocess them. Events that have exceeded the maximum retry threshold are left in Failed
    /// status and skipped to avoid infinite retry loops.
    /// </summary>
    public sealed class WebhookRetryJob : BackgroundService
    {
        private static readonly TimeSpan Interval      = TimeSpan.FromMinutes(5);
        private const int MaxRetryThreshold            = 5;
        private const int BatchSize                    = 20;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<WebhookRetryJob> _logger;

        public WebhookRetryJob(IServiceScopeFactory scopeFactory, ILogger<WebhookRetryJob> logger)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger       = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WebhookRetryJob started. Polling interval: {Interval}.", Interval);

            // Give the application a moment to finish startup before the first run.
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessFailedEventsAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Shutdown in progress — exit gracefully.
                    break;
                }
                catch (Exception ex)
                {
                    // Log unexpected errors but keep the service alive.
                    _logger.LogError(ex, "Unhandled exception in WebhookRetryJob polling loop.");
                }

                try
                {
                    await Task.Delay(Interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("WebhookRetryJob stopped.");
        }

        // ── Private helpers ──────────────────────────────────────────────────────────

        private async Task ProcessFailedEventsAsync(CancellationToken cancellationToken)
        {
            // Each iteration uses its own DI scope so that scoped repositories and services
            // are properly disposed between runs.
            using var scope = _scopeFactory.CreateScope();

            var eventRepository  = scope.ServiceProvider.GetRequiredService<IRepositoryEventRepository>();
            var webhookService   = scope.ServiceProvider.GetRequiredService<IGiteaWebhookService>();

            // Retrieve failed events that have not yet exhausted their retry budget.
            var failedEvents = await eventRepository.GetFailedEventsAsync(MaxRetryThreshold, BatchSize, cancellationToken);

            if (failedEvents.Count == 0)
            {
                _logger.LogDebug("WebhookRetryJob: No failed events eligible for reprocessing.");
                return;
            }

            _logger.LogInformation("WebhookRetryJob: Found {Count} failed event(s) to reprocess.", failedEvents.Count);

            int succeeded = 0;
            int failed    = 0;

            foreach (var evt in failedEvents)
            {
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    await webhookService.ReprocessEventAsync(evt.Id, cancellationToken);
                    succeeded++;
                    _logger.LogInformation(
                        "WebhookRetryJob: Reprocessed event '{EventId}' (delivery={DeliveryId}).",
                        evt.Id, evt.DeliveryId);
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogWarning(
                        ex,
                        "WebhookRetryJob: Failed to reprocess event '{EventId}'. RetryCount={RetryCount}.",
                        evt.Id, evt.RetryCount);
                }
            }

            _logger.LogInformation(
                "WebhookRetryJob: Batch complete. Succeeded={Succeeded}, Failed={Failed}.",
                succeeded, failed);
        }
    }
}
