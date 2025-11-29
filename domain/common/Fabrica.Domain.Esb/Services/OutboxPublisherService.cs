using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Fabrica.Domain.Esb.Models;

namespace Fabrica.Domain.Esb.Services;

/// <summary>
/// Background service that listens for PostgreSQL NOTIFY events on the outbox channel
/// and publishes pending outbox entries to Kafka.
///
/// Each domain ACL should create a derived class that provides the DbContext.
/// </summary>
public abstract class OutboxPublisherService<TContext> : BackgroundService
    where TContext : DbContext
{
    private readonly IServiceProvider _serviceProvider;
    private readonly KafkaProducerService _kafkaProducer;
    private readonly TelemetryService? _telemetryService;
    private readonly ILogger _logger;
    private readonly string _connectionString;
    private readonly string _channelName;
    private readonly string _domainName;

    private NpgsqlConnection? _listenConnection;
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    protected OutboxPublisherService(
        IServiceProvider serviceProvider,
        KafkaProducerService kafkaProducer,
        string connectionString,
        string domainName,
        ILogger logger,
        string channelName = "outbox_events",
        TelemetryService? telemetryService = null)
    {
        _serviceProvider = serviceProvider;
        _kafkaProducer = kafkaProducer;
        _connectionString = connectionString;
        _domainName = domainName;
        _logger = logger;
        _channelName = channelName;
        _telemetryService = telemetryService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "OutboxPublisher starting for domain '{Domain}', listening on channel '{Channel}'",
            _domainName, _channelName);

        _telemetryService?.EmitServiceStarted(TelemetryServiceType.OutboxPublisher);

        // Process any pending events from before service started
        await ProcessPendingEventsAsync(stoppingToken);

        // Start listening for notifications
        await ListenForNotificationsAsync(stoppingToken);
    }

    private async Task ListenForNotificationsAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _listenConnection = new NpgsqlConnection(_connectionString);
                await _listenConnection.OpenAsync(stoppingToken);

                _listenConnection.Notification += async (sender, args) =>
                {
                    _logger.LogDebug(
                        "Received notification on channel '{Channel}': {Payload}",
                        args.Channel, args.Payload);

                    // Process all pending events (not just one)
                    await ProcessPendingEventsAsync(stoppingToken);
                };

                // Subscribe to the channel
                await using var cmd = new NpgsqlCommand($"LISTEN {_channelName}", _listenConnection);
                await cmd.ExecuteNonQueryAsync(stoppingToken);

                _logger.LogInformation(
                    "Now listening for notifications on channel '{Channel}'",
                    _channelName);

                // Wait for notifications
                while (!stoppingToken.IsCancellationRequested)
                {
                    // Wait with timeout so we can check cancellation
                    await _listenConnection.WaitAsync(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in notification listener for domain '{Domain}', reconnecting in 5s...",
                    _domainName);

                await CleanupConnectionAsync();
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        await CleanupConnectionAsync();
    }

    private async Task ProcessPendingEventsAsync(CancellationToken stoppingToken)
    {
        // Use semaphore to prevent concurrent processing
        if (!await _processingSemaphore.WaitAsync(0, stoppingToken))
        {
            _logger.LogDebug("Processing already in progress, skipping");
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TContext>();

            while (!stoppingToken.IsCancellationRequested)
            {
                // Get batch of pending events with row locking
                var pendingEvents = await GetPendingEventsAsync(context, stoppingToken);

                if (pendingEvents.Count == 0)
                {
                    _logger.LogDebug("No pending events to process");
                    break;
                }

                _logger.LogInformation(
                    "Processing {Count} pending outbox events for domain '{Domain}'",
                    pendingEvents.Count, _domainName);

                foreach (var evt in pendingEvents)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    var stopwatch = Stopwatch.StartNew();
                    try
                    {
                        // Mark as processing
                        evt.Status = "processing";
                        await context.SaveChangesAsync(stoppingToken);

                        // Publish to Kafka
                        var success = await _kafkaProducer.PublishAsync(evt, stoppingToken);
                        stopwatch.Stop();

                        // Parse action from event type (e.g., "product.created" -> "created")
                        var action = evt.EventType.Contains('.')
                            ? evt.EventType.Split('.').Last()
                            : evt.EventType;

                        if (success)
                        {
                            evt.Status = "processed";
                            evt.ProcessedAt = DateTime.UtcNow;
                            _logger.LogDebug(
                                "Successfully published event {EventId} ({EventType})",
                                evt.Id, evt.EventType);

                            _telemetryService?.EmitPublishEvent(
                                topic: $"{_domainName}.{evt.EventType}",
                                aggregateType: evt.AggregateType,
                                aggregateId: evt.AggregateId,
                                tenantId: evt.TenantId,
                                action: action,
                                success: true,
                                durationMs: stopwatch.ElapsedMilliseconds);
                        }
                        else
                        {
                            evt.Status = "failed";
                            _logger.LogWarning(
                                "Failed to publish event {EventId} ({EventType})",
                                evt.Id, evt.EventType);

                            _telemetryService?.EmitPublishEvent(
                                topic: $"{_domainName}.{evt.EventType}",
                                aggregateType: evt.AggregateType,
                                aggregateId: evt.AggregateId,
                                tenantId: evt.TenantId,
                                action: action,
                                success: false,
                                durationMs: stopwatch.ElapsedMilliseconds,
                                errorMessage: "Kafka publish failed");
                        }

                        await context.SaveChangesAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        stopwatch.Stop();
                        _logger.LogError(ex,
                            "Error processing event {EventId} ({EventType})",
                            evt.Id, evt.EventType);

                        _telemetryService?.EmitPublishEvent(
                            topic: $"{_domainName}.{evt.EventType}",
                            aggregateType: evt.AggregateType,
                            aggregateId: evt.AggregateId,
                            tenantId: evt.TenantId,
                            action: evt.EventType,
                            success: false,
                            durationMs: stopwatch.ElapsedMilliseconds,
                            errorMessage: ex.Message);

                        evt.Status = "failed";
                        await context.SaveChangesAsync(stoppingToken);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessPendingEventsAsync for domain '{Domain}'", _domainName);
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }

    private async Task<List<OutboxEvent>> GetPendingEventsAsync(
        TContext context,
        CancellationToken stoppingToken)
    {
        // Use raw SQL for FOR UPDATE SKIP LOCKED to prevent duplicate processing
        // in case of multiple service instances
        var sql = @"
            SELECT id, tenant_id, aggregate_type, aggregate_id, event_type,
                   event_data, created_at, processed_at, status
            FROM cdc.outbox
            WHERE status = 'pending'
            ORDER BY created_at ASC
            LIMIT 100
            FOR UPDATE SKIP LOCKED";

        var outboxSet = context.Set<OutboxEvent>();
        return await outboxSet
            .FromSqlRaw(sql)
            .ToListAsync(stoppingToken);
    }

    private async Task CleanupConnectionAsync()
    {
        if (_listenConnection != null)
        {
            try
            {
                await _listenConnection.CloseAsync();
                await _listenConnection.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cleaning up listen connection");
            }
            finally
            {
                _listenConnection = null;
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("OutboxPublisher stopping for domain '{Domain}'", _domainName);
        _telemetryService?.EmitServiceStopped(TelemetryServiceType.OutboxPublisher);
        await CleanupConnectionAsync();
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _processingSemaphore.Dispose();
        _listenConnection?.Dispose();
        base.Dispose();
    }
}
