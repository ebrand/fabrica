using ContentDomainService.Data;
using Fabrica.Domain.Esb.Services;

namespace ContentDomainService.BackgroundServices;

/// <summary>
/// Content domain-specific outbox publisher.
/// Listens for PostgreSQL NOTIFY events and publishes to Kafka.
/// </summary>
public class ContentOutboxPublisher : OutboxPublisherService<ContentDbContext>
{
    public ContentOutboxPublisher(
        IServiceProvider serviceProvider,
        KafkaProducerService kafkaProducer,
        IConfiguration configuration,
        ILogger<ContentOutboxPublisher> logger,
        TelemetryService? telemetryService = null)
        : base(
            serviceProvider,
            kafkaProducer,
            configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection not configured"),
            domainName: "content",
            logger,
            telemetryService: telemetryService)
    {
    }
}
