using ProductDomainService.Data;
using Fabrica.Domain.Esb.Services;

namespace ProductDomainService.BackgroundServices;

/// <summary>
/// Product domain-specific cache subscriber.
/// Listens to Kafka topics defined in cache_config and caches events locally.
/// </summary>
public class ProductCacheSubscriber : CacheSubscriberService<ProductDbContext>
{
    public ProductCacheSubscriber(
        IServiceProvider serviceProvider,
        KafkaConsumerService kafkaConsumer,
        ILogger<ProductCacheSubscriber> logger,
        TelemetryService? telemetryService = null)
        : base(
            serviceProvider,
            kafkaConsumer,
            domainName: "product",
            logger,
            telemetryService)
    {
    }
}
