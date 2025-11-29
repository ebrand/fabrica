using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Fabrica.Domain.Esb.Models;

namespace Fabrica.Domain.Esb.Services;

/// <summary>
/// Background service that subscribes to Kafka topics based on cache_config
/// and stores received events in the cache table.
///
/// Each domain ACL should create a derived class that provides the DbContext.
/// </summary>
public abstract class CacheSubscriberService<TContext> : BackgroundService
    where TContext : DbContext
{
    private readonly IServiceProvider _serviceProvider;
    private readonly KafkaConsumerService _kafkaConsumer;
    private readonly TelemetryService? _telemetryService;
    private readonly ILogger _logger;
    private readonly string _domainName;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(1);
    private readonly TimeSpan _configRefreshInterval = TimeSpan.FromSeconds(10);

    private List<CacheConfig> _activeConfigs = new();
    private DateTime _lastConfigRefresh = DateTime.MinValue;
    private readonly Dictionary<string, List<string>> _consumerTopics = new();

    protected CacheSubscriberService(
        IServiceProvider serviceProvider,
        KafkaConsumerService kafkaConsumer,
        string domainName,
        ILogger logger,
        TelemetryService? telemetryService = null)
    {
        _serviceProvider = serviceProvider;
        _kafkaConsumer = kafkaConsumer;
        _domainName = domainName;
        _logger = logger;
        _telemetryService = telemetryService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "CacheSubscriber starting for domain '{Domain}'",
            _domainName);

        _telemetryService?.EmitServiceStarted(TelemetryServiceType.CacheSubscriber);

        // Initial configuration load
        await RefreshConfigurationsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Periodically refresh configurations
                if (DateTime.UtcNow - _lastConfigRefresh > _configRefreshInterval)
                {
                    await RefreshConfigurationsAsync(stoppingToken);
                }

                // Process messages from all subscribed consumer groups
                await ProcessMessagesAsync(stoppingToken);

                // Small delay between poll cycles
                await Task.Delay(_pollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in CacheSubscriber for domain '{Domain}', retrying in 5s...",
                    _domainName);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("CacheSubscriber stopped for domain '{Domain}'", _domainName);
    }

    private async Task RefreshConfigurationsAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TContext>();

            // Load active cache configurations
            var configs = await context.Set<CacheConfig>()
                .Where(c => c.IsActive)
                .ToListAsync(stoppingToken);

            _activeConfigs = configs;
            _lastConfigRefresh = DateTime.UtcNow;

            // Update subscriptions based on configurations
            UpdateSubscriptions();

            _logger.LogInformation(
                "Refreshed cache configurations for domain '{Domain}': {Count} active configs",
                _domainName, configs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to refresh cache configurations for domain '{Domain}'",
                _domainName);
        }
    }

    private void UpdateSubscriptions()
    {
        // Group configs by consumer group
        var groupedConfigs = _activeConfigs
            .GroupBy(c => c.ConsumerGroup)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var (consumerGroup, configs) in groupedConfigs)
        {
            var topics = new HashSet<string>();

            foreach (var config in configs)
            {
                // Build topic name based on source domain and table
                // Topic format: {source_domain}.{source_table}
                // All events (created/updated/deleted) go to the same topic.
                // The action is in the message's EventType field for filtering.
                if (config.ListenCreate || config.ListenUpdate || config.ListenDelete)
                {
                    topics.Add($"{config.SourceDomain}.{config.SourceTable}");
                }
            }

            if (topics.Count > 0)
            {
                var topicList = topics.ToList();

                // Check if subscription needs updating
                if (!_consumerTopics.TryGetValue(consumerGroup, out var existingTopics) ||
                    !existingTopics.OrderBy(t => t).SequenceEqual(topicList.OrderBy(t => t)))
                {
                    _kafkaConsumer.Subscribe(consumerGroup, topicList);
                    _consumerTopics[consumerGroup] = topicList;

                    _logger.LogInformation(
                        "Updated subscription for consumer group '{Group}': {Topics}",
                        consumerGroup, string.Join(", ", topicList));

                    _telemetryService?.EmitSubscriptionUpdated(consumerGroup, topicList);
                }
            }
        }
    }

    private async Task ProcessMessagesAsync(CancellationToken stoppingToken)
    {
        foreach (var consumerGroup in _consumerTopics.Keys)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                // Consume messages (non-blocking with short timeout)
                var result = _kafkaConsumer.Consume(
                    consumerGroup,
                    TimeSpan.FromMilliseconds(100),
                    stoppingToken);

                if (result?.Message?.Value != null)
                {
                    await ProcessMessageAsync(consumerGroup, result, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing messages for consumer group '{Group}'",
                    consumerGroup);
            }
        }
    }

    private async Task ProcessMessageAsync(
        string consumerGroup,
        Confluent.Kafka.ConsumeResult<string, string> result,
        CancellationToken stoppingToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var message = _kafkaConsumer.DeserializeMessage(result.Message.Value);
        if (message == null)
        {
            _logger.LogWarning(
                "Failed to deserialize message from topic {Topic}",
                result.Topic);
            _kafkaConsumer.Commit(consumerGroup, result);
            return;
        }

        _logger.LogDebug(
            "Processing message from topic {Topic}: EventId={EventId}, Type={EventType}",
            result.Topic, message.EventId, message.EventType);

        // Find matching config to check if we should process this event type
        var config = _activeConfigs.FirstOrDefault(
            c => c.ConsumerGroup == consumerGroup &&
                 c.SourceDomain == message.Domain &&
                 c.SourceTable == message.AggregateType);

        if (config == null)
        {
            // No config found - skip but commit offset
            _logger.LogDebug(
                "No config found for {Domain}.{Table}, skipping",
                message.Domain, message.AggregateType);
            _kafkaConsumer.Commit(consumerGroup, result);
            return;
        }

        // Check if this event type should be processed based on config
        var isCreateEvent = message.EventType.EndsWith(".created");
        var isUpdateEvent = message.EventType.EndsWith(".updated");
        var isDeleteEvent = message.EventType.EndsWith(".deleted");

        var shouldProcess = (isCreateEvent && config.ListenCreate) ||
                           (isUpdateEvent && config.ListenUpdate) ||
                           (isDeleteEvent && config.ListenDelete);

        if (!shouldProcess)
        {
            // Event type not configured for listening - skip but commit offset
            _logger.LogDebug(
                "Skipping event type {EventType} for {Domain}.{Table} (not configured)",
                message.EventType, message.Domain, message.AggregateType);
            _kafkaConsumer.Commit(consumerGroup, result);
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TContext>();

            // Find existing cache entry or create new one
            var cacheSet = context.Set<CacheEntry>();
            var existing = await cacheSet.FirstOrDefaultAsync(
                c => c.SourceDomain == message.Domain &&
                     c.SourceTable == message.AggregateType &&
                     c.AggregateId == message.AggregateId,
                stoppingToken);

            if (existing != null)
            {
                if (isDeleteEvent)
                {
                    // Hard delete - remove the cache entry
                    cacheSet.Remove(existing);
                    _logger.LogDebug(
                        "Deleted cache entry for {Domain}.{Table} ID={AggregateId}",
                        message.Domain, message.AggregateType, message.AggregateId);
                }
                else
                {
                    // Update existing entry
                    existing.LastEventType = message.EventType;
                    existing.CacheData = message.EventData;
                    existing.Version++;
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.SourceEventId = message.EventId;
                    existing.SourceEventTime = DateTime.SpecifyKind(message.Timestamp, DateTimeKind.Utc);

                    // Update expiration if configured
                    if (config.CacheTtlSeconds != null)
                    {
                        existing.ExpiresAt = DateTime.UtcNow.AddSeconds(config.CacheTtlSeconds.Value);
                    }

                    _logger.LogDebug(
                        "Updated cache entry for {Domain}.{Table} ID={AggregateId} (version {Version})",
                        message.Domain, message.AggregateType, message.AggregateId, existing.Version);
                }
            }
            else if (!isDeleteEvent)
            {
                // Create new entry (don't create for delete events with no existing entry)
                var newEntry = new CacheEntry
                {
                    SourceDomain = message.Domain,
                    SourceTable = message.AggregateType,
                    AggregateId = message.AggregateId,
                    TenantId = message.TenantId,
                    LastEventType = message.EventType,
                    CacheData = message.EventData,
                    Version = 1,
                    IsDeleted = false,
                    CachedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    SourceEventId = message.EventId,
                    SourceEventTime = DateTime.SpecifyKind(message.Timestamp, DateTimeKind.Utc),
                    ExpiresAt = config.CacheTtlSeconds != null
                        ? DateTime.UtcNow.AddSeconds(config.CacheTtlSeconds.Value)
                        : null
                };

                cacheSet.Add(newEntry);

                _logger.LogDebug(
                    "Created cache entry for {Domain}.{Table} ID={AggregateId}",
                    message.Domain, message.AggregateType, message.AggregateId);
            }

            await context.SaveChangesAsync(stoppingToken);
            stopwatch.Stop();

            // Commit offset after successful processing
            _kafkaConsumer.Commit(consumerGroup, result);

            // Parse action from event type
            var action = message.EventType.Contains('.')
                ? message.EventType.Split('.').Last()
                : message.EventType;

            _telemetryService?.EmitConsumeEvent(
                topic: result.Topic,
                aggregateType: message.AggregateType,
                aggregateId: message.AggregateId,
                tenantId: message.TenantId,
                action: action,
                success: true,
                durationMs: stopwatch.ElapsedMilliseconds,
                offset: result.Offset.Value,
                partition: result.Partition.Value);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Failed to process message EventId={EventId} from topic {Topic}",
                message.EventId, result.Topic);

            var action = message.EventType.Contains('.')
                ? message.EventType.Split('.').Last()
                : message.EventType;

            _telemetryService?.EmitConsumeEvent(
                topic: result.Topic,
                aggregateType: message.AggregateType,
                aggregateId: message.AggregateId,
                tenantId: message.TenantId,
                action: action,
                success: false,
                durationMs: stopwatch.ElapsedMilliseconds,
                errorMessage: ex.Message);
            // Don't commit - message will be reprocessed
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CacheSubscriber stopping for domain '{Domain}'", _domainName);
        _telemetryService?.EmitServiceStopped(TelemetryServiceType.CacheSubscriber);

        // Close all consumers
        foreach (var consumerGroup in _consumerTopics.Keys)
        {
            _kafkaConsumer.CloseConsumer(consumerGroup);
        }

        await base.StopAsync(cancellationToken);
    }
}
