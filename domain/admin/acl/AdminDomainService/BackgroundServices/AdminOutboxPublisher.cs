using AdminDomainService.Data;
using Fabrica.Domain.Esb.Services;

namespace AdminDomainService.BackgroundServices;

/// <summary>
/// Admin domain-specific outbox publisher.
/// Listens for PostgreSQL NOTIFY events and publishes to Kafka.
/// </summary>
public class AdminOutboxPublisher : OutboxPublisherService<AdminDbContext>
{
    public AdminOutboxPublisher(
        IServiceProvider serviceProvider,
        KafkaProducerService kafkaProducer,
        IConfiguration configuration,
        ILogger<AdminOutboxPublisher> logger,
        TelemetryService? telemetryService = null)
        : base(
            serviceProvider,
            kafkaProducer,
            configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection not configured"),
            domainName: "admin",
            logger,
            telemetryService: telemetryService)
    {
    }
}
