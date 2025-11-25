using Microsoft.EntityFrameworkCore;
using ContentDomainService.Data;
using System.Text.Json;
using Fabrica.Domain.Esb.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HttpClient for config retrieval
builder.Services.AddHttpClient();

// Retrieve database connection string from ACL Admin service
var connectionString = await GetDatabaseConnectionStringAsync(builder.Configuration);

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

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
