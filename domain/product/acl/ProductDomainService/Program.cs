using Microsoft.EntityFrameworkCore;
using ProductDomainService.Data;
using ProductDomainService.BackgroundServices;
using System.Text.Json;
using Fabrica.Domain.Esb.Extensions;
using Fabrica.Domain.Esb.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HttpClient for config retrieval
builder.Services.AddHttpClient();

// Retrieve database connection string from ACL Admin service
var connectionString = await GetDatabaseConnectionStringAsync(builder.Configuration);

// Store connection string in configuration for background services
builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;

// Add DbContext with outbox interceptor
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseNpgsql(connectionString)
           .AddOutboxInterceptor());

// Retrieve Kafka configuration from ACL Admin service
var kafkaBootstrapServers = await GetKafkaConfigAsync(builder.Configuration);

// Register Kafka producer service
builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<KafkaProducerService>>();
    return new KafkaProducerService(kafkaBootstrapServers, "product", logger);
});

// Register Kafka consumer service
builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<KafkaConsumerService>>();
    return new KafkaConsumerService(kafkaBootstrapServers, "product", logger);
});

// Register Telemetry service for ESB monitoring
builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<TelemetryService>>();
    return new TelemetryService(kafkaBootstrapServers, "product", logger);
});

// Register outbox publisher background service
builder.Services.AddHostedService<ProductOutboxPublisher>();

// Register cache subscriber background service
builder.Services.AddHostedService<ProductCacheSubscriber>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();

// Helper method to retrieve database connection string from ACL Admin service
static async Task<string> GetDatabaseConnectionStringAsync(IConfiguration configuration)
{
    var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("Startup");

    try
    {
        var aclAdminUrl = configuration["AclAdmin:Url"] ?? "http://acl-admin:3600";
        var configUrl = $"{aclAdminUrl}/api/config/database/product";

        logger.LogInformation("Fetching database configuration from ACL Admin service: {Url}", configUrl);

        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        var response = await httpClient.GetAsync(configUrl);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("connectionString", out var connStringElement))
            {
                var connString = connStringElement.GetString();
                logger.LogInformation("Successfully retrieved database configuration from ACL Admin service");
                return connString ?? throw new Exception("Connection string is null");
            }
        }

        throw new Exception($"Failed to retrieve config from ACL Admin: {response.StatusCode}");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to retrieve database configuration from ACL Admin, using fallback");

        // Fallback to local configuration
        var fallbackConnString = configuration.GetConnectionString("ProductDb")
            ?? "Host=postgres;Port=5432;Database=fabrica-product-db;Username=fabrica_admin;Password=fabrica_dev_password";

        logger.LogInformation("Using fallback database configuration");
        return fallbackConnString;
    }
}

// Helper method to retrieve Kafka configuration from ACL Admin service
static async Task<string> GetKafkaConfigAsync(IConfiguration configuration)
{
    var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("Startup");

    try
    {
        var aclAdminUrl = configuration["AclAdmin:Url"] ?? "http://acl-admin:3600";
        var configUrl = $"{aclAdminUrl}/api/config/kafka";

        logger.LogInformation("Fetching Kafka configuration from ACL Admin service: {Url}", configUrl);

        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        var response = await httpClient.GetAsync(configUrl);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("bootstrapServers", out var bootstrapServersElement))
            {
                var bootstrapServers = bootstrapServersElement.GetString();
                logger.LogInformation("Successfully retrieved Kafka configuration from ACL Admin service");
                return bootstrapServers ?? "kafka:9092";
            }
        }

        throw new Exception($"Failed to retrieve Kafka config from ACL Admin: {response.StatusCode}");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to retrieve Kafka configuration from ACL Admin, using fallback");

        // Fallback to environment variable or default
        var fallbackKafka = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "kafka:9092";

        logger.LogInformation("Using fallback Kafka configuration: {Servers}", fallbackKafka);
        return fallbackKafka;
    }
}
