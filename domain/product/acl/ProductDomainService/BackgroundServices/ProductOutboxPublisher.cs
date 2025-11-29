using ProductDomainService.Data;
using Fabrica.Domain.Esb.Services;

namespace ProductDomainService.BackgroundServices;

/// <summary>
/// Product domain-specific outbox publisher.
/// Listens for PostgreSQL NOTIFY events and publishes to Kafka.
/// </summary>
public class ProductOutboxPublisher : OutboxPublisherService<ProductDbContext>
{
    public ProductOutboxPublisher(
        IServiceProvider serviceProvider,
        KafkaProducerService kafkaProducer,
        IConfiguration configuration,
        ILogger<ProductOutboxPublisher> logger,
        TelemetryService? telemetryService = null)
        : base(
            serviceProvider,
            kafkaProducer,
            configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection not configured"),
            domainName: "product",
            logger,
            telemetryService: telemetryService)
    {
    }
}
