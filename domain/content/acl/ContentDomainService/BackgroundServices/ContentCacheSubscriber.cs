using ContentDomainService.Data;
using Fabrica.Domain.Esb.Services;

namespace ContentDomainService.BackgroundServices;

/// <summary>
/// Content domain-specific cache subscriber.
/// Listens to Kafka topics defined in cache_config and caches events locally.
/// </summary>
public class ContentCacheSubscriber : CacheSubscriberService<ContentDbContext>
{
    public ContentCacheSubscriber(
        IServiceProvider serviceProvider,
        KafkaConsumerService kafkaConsumer,
        ILogger<ContentCacheSubscriber> logger,
        TelemetryService? telemetryService = null)
        : base(
            serviceProvider,
            kafkaConsumer,
            domainName: "content",
            logger,
            telemetryService)
    {
    }
}
