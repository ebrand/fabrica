using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Fabrica.Domain.Esb.Models;

namespace Fabrica.Domain.Esb.Services;

/// <summary>
/// Kafka producer service for publishing domain events.
/// Thread-safe and designed for singleton use.
/// </summary>
public class KafkaProducerService : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly string _domainName;
    private bool _disposed;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public KafkaProducerService(
        string bootstrapServers,
        string domainName,
        ILogger<KafkaProducerService> logger)
    {
        _domainName = domainName;
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            ClientId = $"{domainName}-producer",
            Acks = Acks.All,                    // Wait for all replicas to acknowledge
            EnableIdempotence = true,           // Exactly-once semantics
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 100,
            LingerMs = 5,                       // Small batch window for throughput
            CompressionType = CompressionType.Snappy
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka producer error: {Reason}", e.Reason))
            .SetLogHandler((_, m) => _logger.LogDebug("Kafka: {Message}", m.Message))
            .Build();

        _logger.LogInformation("Kafka producer initialized for domain '{Domain}' connecting to {Servers}",
            domainName, bootstrapServers);
    }

    /// <summary>
    /// Publishes an outbox event to Kafka.
    /// Topic format: {domain}.{aggregate_type}.{action}
    /// </summary>
    public async Task<bool> PublishAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        var topic = GetTopicName(outboxEvent);
        var key = outboxEvent.AggregateId.ToString();

        var message = new KafkaMessage
        {
            EventId = outboxEvent.Id,
            TenantId = outboxEvent.TenantId,
            AggregateType = outboxEvent.AggregateType,
            AggregateId = outboxEvent.AggregateId,
            EventType = outboxEvent.EventType,
            EventData = outboxEvent.EventData,
            Timestamp = outboxEvent.CreatedAt,
            Domain = _domainName
        };

        var value = JsonSerializer.Serialize(message, JsonOptions);

        try
        {
            var result = await _producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = key,
                Value = value,
                Headers = new Headers
                {
                    { "event-id", System.Text.Encoding.UTF8.GetBytes(outboxEvent.Id.ToString()) },
                    { "event-type", System.Text.Encoding.UTF8.GetBytes(outboxEvent.EventType) },
                    { "tenant-id", System.Text.Encoding.UTF8.GetBytes(outboxEvent.TenantId) },
                    { "domain", System.Text.Encoding.UTF8.GetBytes(_domainName) }
                }
            }, cancellationToken);

            _logger.LogDebug(
                "Published event {EventId} to topic {Topic} partition {Partition} offset {Offset}",
                outboxEvent.Id, topic, result.Partition.Value, result.Offset.Value);

            return true;
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex,
                "Failed to publish event {EventId} to topic {Topic}: {Error}",
                outboxEvent.Id, topic, ex.Error.Reason);
            return false;
        }
    }

    /// <summary>
    /// Publishes multiple events in a batch.
    /// </summary>
    public async Task<int> PublishBatchAsync(
        IEnumerable<OutboxEvent> events,
        CancellationToken cancellationToken = default)
    {
        var successCount = 0;
        var tasks = new List<Task<bool>>();

        foreach (var evt in events)
        {
            tasks.Add(PublishAsync(evt, cancellationToken));
        }

        var results = await Task.WhenAll(tasks);
        successCount = results.Count(r => r);

        // Flush to ensure all messages are sent
        _producer.Flush(TimeSpan.FromSeconds(10));

        return successCount;
    }

    /// <summary>
    /// Gets the Kafka topic name for an outbox event.
    /// Format: {domain}.{aggregate_type}
    /// Example: admin.user, product.category, product.product
    /// All events (created/updated/deleted) for an aggregate go to the same topic.
    /// The action is included in the message's EventType field for filtering.
    /// </summary>
    private string GetTopicName(OutboxEvent evt)
    {
        // Use aggregate type for topic name (matches OutboxConfig.TopicName format)
        return $"{_domainName}.{evt.AggregateType}";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();

        _logger.LogInformation("Kafka producer disposed for domain '{Domain}'", _domainName);
    }
}

/// <summary>
/// Message format for Kafka events
/// </summary>
public class KafkaMessage
{
    public Guid EventId { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string AggregateType { get; set; } = string.Empty;
    public Guid AggregateId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = "{}";
    public DateTime Timestamp { get; set; }
}
