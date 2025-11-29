namespace Fabrica.Domain.Esb.Models;

/// <summary>
/// Represents a telemetry event from ESB producers and consumers.
/// Published to the esb.telemetry Kafka topic for real-time monitoring.
/// </summary>
public class TelemetryEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Domain { get; set; } = string.Empty;
    public TelemetryEventType EventType { get; set; }
    public TelemetryServiceType ServiceType { get; set; }

    // Event details
    public string Topic { get; set; } = string.Empty;
    public string AggregateType { get; set; } = string.Empty;
    public Guid? AggregateId { get; set; }
    public string? TenantId { get; set; }
    public string Action { get; set; } = string.Empty;

    // Status
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }

    // Performance metrics
    public long? DurationMs { get; set; }
    public int? BatchSize { get; set; }
    public long? Offset { get; set; }
    public int? Partition { get; set; }
}

public enum TelemetryEventType
{
    // Producer events
    EventPublished,
    EventPublishFailed,
    BatchPublished,

    // Consumer events
    EventReceived,
    EventProcessed,
    EventProcessFailed,

    // Service lifecycle
    ServiceStarted,
    ServiceStopped,
    SubscriptionUpdated,
    ConfigurationRefreshed
}

public enum TelemetryServiceType
{
    OutboxPublisher,
    CacheSubscriber
}
