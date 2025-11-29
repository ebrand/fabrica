using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using ContentDomainService.Data;
using ContentDomainService.BackgroundServices;
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
builder.Services.AddDbContext<ContentDbContext>(options =>
    options.UseNpgsql(connectionString)
           .AddOutboxInterceptor());

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

// Configure Kafka
var kafkaBootstrapServers = builder.Configuration["Kafka:BootstrapServers"]
    ?? Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS")
    ?? "kafka:9092";

// Register Kafka producer service
builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<KafkaProducerService>>();
    return new KafkaProducerService(kafkaBootstrapServers, "content", logger);
});

// Register Kafka consumer service
builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<KafkaConsumerService>>();
    return new KafkaConsumerService(kafkaBootstrapServers, "content", logger);
});

// Register Telemetry service for ESB monitoring
builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<TelemetryService>>();
    return new TelemetryService(kafkaBootstrapServers, "content", logger);
});

// Register outbox publisher background service
builder.Services.AddHostedService<ContentOutboxPublisher>();

// Register cache subscriber background service
builder.Services.AddHostedService<ContentCacheSubscriber>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// Serve uploaded files
var uploadPath = builder.Configuration["UPLOAD_PATH"] ?? "/app/uploads";
if (!Directory.Exists(uploadPath))
{
    Directory.CreateDirectory(uploadPath);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadPath),
    RequestPath = "/uploads"
});

app.UseAuthorization();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "acl-content", timestamp = DateTime.UtcNow }));

app.MapControllers();

app.Run();

// Helper method to retrieve database connection string from ACL Admin service
static async Task<string> GetDatabaseConnectionStringAsync(IConfiguration configuration)
{
    var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("Startup");

    try
    {
        var aclAdminUrl = configuration["AclAdmin:Url"] ?? "http://acl-admin:3600";
        var configUrl = $"{aclAdminUrl}/api/config/database/content";

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
        var fallbackConnString = configuration.GetConnectionString("ContentDb")
            ?? "Host=postgres;Port=5432;Database=fabrica-content-db;Username=fabrica_admin;Password=fabrica_dev_password";

        logger.LogInformation("Using fallback database configuration");
        return fallbackConnString;
    }
}
