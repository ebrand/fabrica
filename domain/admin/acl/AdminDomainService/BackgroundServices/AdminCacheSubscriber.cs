using AdminDomainService.Data;
using Fabrica.Domain.Esb.Services;

namespace AdminDomainService.BackgroundServices;

/// <summary>
/// Admin domain-specific cache subscriber.
/// Listens to Kafka topics defined in cache_config and caches events locally.
/// </summary>
public class AdminCacheSubscriber : CacheSubscriberService<AdminDbContext>
{
    public AdminCacheSubscriber(
        IServiceProvider serviceProvider,
        KafkaConsumerService kafkaConsumer,
        ILogger<AdminCacheSubscriber> logger,
        TelemetryService? telemetryService = null)
        : base(
            serviceProvider,
            kafkaConsumer,
            domainName: "admin",
            logger,
            telemetryService)
    {
    }
}
