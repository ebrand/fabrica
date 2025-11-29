using System.Text.Json;
using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using AdminBFF.Hubs;

namespace AdminBFF.BackgroundServices;

/// <summary>
/// Background service that consumes ESB telemetry events from Kafka
/// and broadcasts them to connected SignalR clients.
/// </summary>
public class TelemetryConsumer : BackgroundService
{
    private readonly IHubContext<TelemetryHub> _hubContext;
    private readonly ILogger<TelemetryConsumer> _logger;
    private readonly string _bootstrapServers;
    private IConsumer<string, string>? _consumer;

    private const string TelemetryTopic = "esb.telemetry";
    private const string ConsumerGroup = "admin-bff-telemetry";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public TelemetryConsumer(
        IHubContext<TelemetryHub> hubContext,
        IConfiguration configuration,
        ILogger<TelemetryConsumer> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
        _bootstrapServers = configuration["Kafka:BootstrapServers"]
            ?? Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS")
            ?? "kafka:9092";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TelemetryConsumer starting, connecting to {Servers}", _bootstrapServers);

        // Wait a bit for Kafka to be ready
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Latest, // Only get new messages
            EnableAutoCommit = true,
            EnableAutoOffsetStore = true
        };

        try
        {
            _consumer = new ConsumerBuilder<string, string>(config)
                .SetErrorHandler((_, e) => _logger.LogWarning("Kafka consumer error: {Reason}", e.Reason))
                .Build();

            _consumer.Subscribe(TelemetryTopic);
            _logger.LogInformation("Subscribed to topic: {Topic}", TelemetryTopic);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(TimeSpan.FromMilliseconds(100));

                    if (result?.Message?.Value != null)
                    {
                        await ProcessMessageAsync(result.Message.Value, stoppingToken);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogWarning(ex, "Error consuming message");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in TelemetryConsumer");
        }
        finally
        {
            _consumer?.Close();
            _consumer?.Dispose();
        }
    }

    private async Task ProcessMessageAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Received telemetry message: {Message}", message);

            var telemetryEvent = JsonSerializer.Deserialize<TelemetryEventDto>(message, JsonOptions);
            if (telemetryEvent == null) return;

            _logger.LogInformation(
                "Broadcasting telemetry event: {Domain} {EventType} {ServiceType}",
                telemetryEvent.Domain, telemetryEvent.EventType, telemetryEvent.ServiceType);

            // Broadcast to all connected clients
            await _hubContext.Clients.All.SendAsync(
                "TelemetryEvent",
                telemetryEvent,
                cancellationToken);

            // Also send to domain-specific group
            await _hubContext.Clients.Group($"domain:{telemetryEvent.Domain}")
                .SendAsync("TelemetryEvent", telemetryEvent, cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize telemetry message: {Message}", message);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("TelemetryConsumer stopping");
        _consumer?.Close();
        await base.StopAsync(cancellationToken);
    }
}

/// <summary>
/// DTO for telemetry events received from Kafka.
/// </summary>
public class TelemetryEventDto
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string AggregateType { get; set; } = string.Empty;
    public Guid? AggregateId { get; set; }
    public string? TenantId { get; set; }
    public string Action { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public long? DurationMs { get; set; }
    public int? BatchSize { get; set; }
    public long? Offset { get; set; }
    public int? Partition { get; set; }
}
