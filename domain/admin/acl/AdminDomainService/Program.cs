using AdminDomainService.Data;
using AdminDomainService.Services;
using AdminDomainService.BackgroundServices;
using Microsoft.EntityFrameworkCore;
using Fabrica.Domain.Esb.Extensions;
using Fabrica.Domain.Esb.Services;

var builder = WebApplication.CreateBuilder(args);

// Fetch configuration directly from Vault at startup
try
{
    Console.WriteLine("📡 Fetching configuration from Vault...");

    // Create a temporary VaultService to fetch configuration
    var tempConfig = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .Build();

    var loggerFactory = LoggerFactory.Create(config => config.AddConsole());
    var logger = loggerFactory.CreateLogger<VaultService>();
    var vaultService = new VaultService(tempConfig, logger);

    // Fetch secrets from Vault
    var dbData = await vaultService.GetSecretDataAsync("infrastructure/postgres");
    var kafkaData = await vaultService.GetSecretDataAsync("infrastructure/kafka");
    var redisData = await vaultService.GetSecretDataAsync("infrastructure/redis");
    var consulData = await vaultService.GetSecretDataAsync("shared/consul");

    // Build connection string and add to configuration
    builder.Configuration["ConnectionStrings:DefaultConnection"] = $"Host={dbData["host"]};Port={dbData["port"]};Database={dbData["database"]};Username={dbData["user"]};Password={dbData["password"]}";
    builder.Configuration["Kafka:BootstrapServers"]              = kafkaData["bootstrap_servers"]?.ToString() ?? "kafka:9092";
    builder.Configuration["Redis:ConnectionString"]              = $"{redisData["host"] ?? "redis"}:{redisData["port"] ?? "6379"}";
    builder.Configuration["Consul:Host"]                         = consulData["host"]?.ToString() ?? "consul";
    builder.Configuration["Consul:Port"]                         = consulData["port"]?.ToString() ?? "8500";

    Console.WriteLine("✅ Successfully loaded configuration from Vault");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️  Failed to load config from Vault: {ex.Message}");
    Console.WriteLine("⚠️  Falling back to environment variables");
}

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? "Host=postgres;Port=5432;Database=fabrica-admin-db;Username=fabrica_admin;Password=fabrica_dev_password";

builder.Services.AddDbContext<AdminDbContext>(options =>
    options.UseNpgsql(connectionString)
           .AddOutboxInterceptor());

// Register VaultService for fetching configuration from Vault
builder.Services.AddSingleton<VaultService>();

// Register ConsulService for service discovery
builder.Services.AddSingleton<ConsulService>();

// Register Kafka producer service
var kafkaBootstrapServers = builder.Configuration["Kafka:BootstrapServers"]
    ?? Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS")
    ?? "kafka:9092";

builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<KafkaProducerService>>();
    return new KafkaProducerService(kafkaBootstrapServers, "admin", logger);
});

// Register Kafka consumer service
builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<KafkaConsumerService>>();
    return new KafkaConsumerService(kafkaBootstrapServers, "admin", logger);
});

// Register Telemetry service for ESB monitoring
builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<TelemetryService>>();
    return new TelemetryService(kafkaBootstrapServers, "admin", logger);
});

// Register outbox publisher background service
builder.Services.AddHostedService<AdminOutboxPublisher>();

// Register cache subscriber background service
builder.Services.AddHostedService<AdminCacheSubscriber>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => new
{
    status = "ok",
    service = "admin-domain-service",
    timestamp = DateTime.UtcNow
}).WithName("HealthCheck");

app.Run();
