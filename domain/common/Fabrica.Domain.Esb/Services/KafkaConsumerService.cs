using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Fabrica.Domain.Esb.Models;

namespace Fabrica.Domain.Esb.Services;

/// <summary>
/// Kafka consumer service for subscribing to domain events.
/// Manages consumer lifecycle and message deserialization.
/// </summary>
public class KafkaConsumerService : IDisposable
{
    private readonly string _bootstrapServers;
    private readonly string _domainName;
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly Dictionary<string, IConsumer<string, string>> _consumers = new();
    private readonly object _lock = new();
    private bool _disposed;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public KafkaConsumerService(
        string bootstrapServers,
        string domainName,
        ILogger<KafkaConsumerService> logger)
    {
        _bootstrapServers = bootstrapServers;
        _domainName = domainName;
        _logger = logger;

        _logger.LogInformation(
            "Kafka consumer service initialized for domain '{Domain}' connecting to {Servers}",
            domainName, bootstrapServers);
    }

    /// <summary>
    /// Gets or creates a consumer for the specified consumer group.
    /// </summary>
    public IConsumer<string, string> GetOrCreateConsumer(string consumerGroup)
    {
        lock (_lock)
        {
            if (_consumers.TryGetValue(consumerGroup, out var existing))
            {
                return existing;
            }

            var config = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = consumerGroup,
                ClientId = $"{_domainName}-consumer-{consumerGroup}",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,  // Manual commit for reliability
                EnableAutoOffsetStore = false,
                MaxPollIntervalMs = 300000,  // 5 minutes
                SessionTimeoutMs = 45000,
                HeartbeatIntervalMs = 3000
            };

            var consumer = new ConsumerBuilder<string, string>(config)
                .SetErrorHandler((_, e) => _logger.LogError(
                    "Kafka consumer error for group {Group}: {Reason}",
                    consumerGroup, e.Reason))
                .SetLogHandler((_, m) => _logger.LogDebug("Kafka: {Message}", m.Message))
                .SetPartitionsAssignedHandler((c, partitions) =>
                {
                    _logger.LogInformation(
                        "Consumer group {Group} assigned partitions: {Partitions}",
                        consumerGroup, string.Join(", ", partitions.Select(p => $"{p.Topic}[{p.Partition}]")));
                })
                .SetPartitionsRevokedHandler((c, partitions) =>
                {
                    _logger.LogInformation(
                        "Consumer group {Group} revoked partitions: {Partitions}",
                        consumerGroup, string.Join(", ", partitions.Select(p => $"{p.Topic}[{p.Partition}]")));
                })
                .Build();

            _consumers[consumerGroup] = consumer;

            _logger.LogInformation(
                "Created Kafka consumer for group '{Group}' in domain '{Domain}'",
                consumerGroup, _domainName);

            return consumer;
        }
    }

    /// <summary>
    /// Subscribes a consumer to the specified topics.
    /// </summary>
    public void Subscribe(string consumerGroup, IEnumerable<string> topics)
    {
        var consumer = GetOrCreateConsumer(consumerGroup);
        var topicList = topics.ToList();

        consumer.Subscribe(topicList);

        _logger.LogInformation(
            "Consumer group '{Group}' subscribed to topics: {Topics}",
            consumerGroup, string.Join(", ", topicList));
    }

    /// <summary>
    /// Consumes a single message from the specified consumer group.
    /// Returns null if no message is available within the timeout.
    /// </summary>
    public ConsumeResult<string, string>? Consume(
        string consumerGroup,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var consumer = GetOrCreateConsumer(consumerGroup);

        try
        {
            var result = consumer.Consume(timeout);
            return result;
        }
        catch (ConsumeException ex)
        {
            _logger.LogError(ex,
                "Error consuming message for group {Group}: {Error}",
                consumerGroup, ex.Error.Reason);
            return null;
        }
    }

    /// <summary>
    /// Commits the offset for a consumed message.
    /// </summary>
    public void Commit(string consumerGroup, ConsumeResult<string, string> result)
    {
        var consumer = GetOrCreateConsumer(consumerGroup);

        try
        {
            consumer.Commit(result);
            _logger.LogDebug(
                "Committed offset {Offset} for topic {Topic} partition {Partition}",
                result.Offset, result.Topic, result.Partition);
        }
        catch (KafkaException ex)
        {
            _logger.LogError(ex,
                "Error committing offset for group {Group}: {Error}",
                consumerGroup, ex.Error.Reason);
        }
    }

    /// <summary>
    /// Deserializes a Kafka message value into a KafkaMessage object.
    /// </summary>
    public KafkaMessage? DeserializeMessage(string value)
    {
        try
        {
            return JsonSerializer.Deserialize<KafkaMessage>(value, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Kafka message: {Value}", value);
            return null;
        }
    }

    /// <summary>
    /// Closes a specific consumer.
    /// </summary>
    public void CloseConsumer(string consumerGroup)
    {
        lock (_lock)
        {
            if (_consumers.TryGetValue(consumerGroup, out var consumer))
            {
                try
                {
                    consumer.Close();
                    consumer.Dispose();
                    _consumers.Remove(consumerGroup);
                    _logger.LogInformation("Closed consumer for group '{Group}'", consumerGroup);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing consumer for group '{Group}'", consumerGroup);
                }
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_lock)
        {
            foreach (var kvp in _consumers)
            {
                try
                {
                    kvp.Value.Close();
                    kvp.Value.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing consumer for group '{Group}'", kvp.Key);
                }
            }
            _consumers.Clear();
        }

        _logger.LogInformation("Kafka consumer service disposed for domain '{Domain}'", _domainName);
    }
}
