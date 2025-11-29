using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Fabrica.Domain.Esb.Models;

namespace Fabrica.Domain.Esb.Services;

/// <summary>
/// Service for publishing ESB telemetry events to Kafka.
/// These events are consumed by the Admin BFF and broadcast via SignalR.
/// </summary>
public class TelemetryService : IDisposable
{
    private readonly IProducer<string, string>? _producer;
    private readonly ILogger<TelemetryService> _logger;
    private readonly string _domainName;
    private readonly bool _isEnabled;
    private bool _disposed;

    private const string TelemetryTopic = "esb.telemetry";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public TelemetryService(
        string bootstrapServers,
        string domainName,
        ILogger<TelemetryService> logger,
        bool isEnabled = true)
    {
        _domainName = domainName;
        _logger = logger;
        _isEnabled = isEnabled;

        if (!_isEnabled)
        {
            _logger.LogInformation("Telemetry service disabled for domain '{Domain}'", domainName);
            return;
        }

        try
        {
            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                ClientId = $"{domainName}-telemetry",
                Acks = Acks.Leader,  // Telemetry doesn't need strong guarantees
                LingerMs = 10,
                BatchSize = 16384
            };

            _producer = new ProducerBuilder<string, string>(config)
                .SetErrorHandler((_, e) => _logger.LogWarning("Telemetry producer error: {Reason}", e.Reason))
                .Build();

            _logger.LogInformation("Telemetry service initialized for domain '{Domain}'", domainName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize telemetry producer for domain '{Domain}'", domainName);
            _isEnabled = false;
        }
    }

    public void EmitEvent(TelemetryEvent telemetryEvent)
    {
        if (!_isEnabled || _producer == null) return;

        try
        {
            telemetryEvent.Domain = _domainName;
            var value = JsonSerializer.Serialize(telemetryEvent, JsonOptions);

            // Fire and forget - we don't wait for telemetry acknowledgement
            _producer.Produce(TelemetryTopic, new Message<string, string>
            {
                Key = _domainName,
                Value = value
            });
        }
        catch (Exception ex)
        {
            // Don't let telemetry failures affect main processing
            _logger.LogDebug(ex, "Failed to emit telemetry event");
        }
    }

    public void EmitPublishEvent(
        string topic,
        string aggregateType,
        Guid aggregateId,
        string tenantId,
        string action,
        bool success,
        long? durationMs = null,
        long? offset = null,
        int? partition = null,
        string? errorMessage = null)
    {
        EmitEvent(new TelemetryEvent
        {
            EventType = success ? TelemetryEventType.EventPublished : TelemetryEventType.EventPublishFailed,
            ServiceType = TelemetryServiceType.OutboxPublisher,
            Topic = topic,
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            TenantId = tenantId,
            Action = action,
            Success = success,
            DurationMs = durationMs,
            Offset = offset,
            Partition = partition,
            ErrorMessage = errorMessage
        });
    }

    public void EmitConsumeEvent(
        string topic,
        string aggregateType,
        Guid aggregateId,
        string tenantId,
        string action,
        bool success,
        long? durationMs = null,
        long? offset = null,
        int? partition = null,
        string? errorMessage = null)
    {
        EmitEvent(new TelemetryEvent
        {
            EventType = success ? TelemetryEventType.EventProcessed : TelemetryEventType.EventProcessFailed,
            ServiceType = TelemetryServiceType.CacheSubscriber,
            Topic = topic,
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            TenantId = tenantId,
            Action = action,
            Success = success,
            DurationMs = durationMs,
            Offset = offset,
            Partition = partition,
            ErrorMessage = errorMessage
        });
    }

    public void EmitServiceStarted(TelemetryServiceType serviceType)
    {
        EmitEvent(new TelemetryEvent
        {
            EventType = TelemetryEventType.ServiceStarted,
            ServiceType = serviceType,
            Success = true
        });
    }

    public void EmitServiceStopped(TelemetryServiceType serviceType)
    {
        EmitEvent(new TelemetryEvent
        {
            EventType = TelemetryEventType.ServiceStopped,
            ServiceType = serviceType,
            Success = true
        });
    }

    public void EmitSubscriptionUpdated(string consumerGroup, IEnumerable<string> topics)
    {
        EmitEvent(new TelemetryEvent
        {
            EventType = TelemetryEventType.SubscriptionUpdated,
            ServiceType = TelemetryServiceType.CacheSubscriber,
            Topic = string.Join(", ", topics),
            Action = consumerGroup,
            Success = true
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_producer != null)
        {
            _producer.Flush(TimeSpan.FromSeconds(5));
            _producer.Dispose();
        }
    }
}
