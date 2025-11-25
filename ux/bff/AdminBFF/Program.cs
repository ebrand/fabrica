using AdminBFF.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Admin BFF API",
        Version = "v1",
        Description = "Backend-for-Frontend aggregating data from domain services for the Admin Shell"
    });
});

// Add CORS - allow any origin, method, and header
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add IHttpClientFactory for all services
builder.Services.AddHttpClient();

// Register ConfigurationService as singleton
builder.Services.AddSingleton<ConfigurationService>();

// Add HTTP client for AdminServiceClient
builder.Services.AddHttpClient<AdminServiceClient>();

// Add AdminServiceClient as scoped
builder.Services.AddScoped<AdminServiceClient>();

// Add ServicesRegistry
builder.Services.AddSingleton<ServicesRegistry>();

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Initialize configuration on startup
var logger = app.Services.GetRequiredService<ILogger<Program>>();
try
{
    var configService = app.Services.GetRequiredService<ConfigurationService>();
    await configService.FetchConfigAsync();
    logger.LogInformation("Configuration initialized successfully");
}
catch (Exception ex)
{
    logger.LogWarning(ex, "Failed to initialize configuration, will use fallback values");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Admin BFF API v1");
    });
}

// Use CORS
app.UseCors();

// Map controllers
app.MapControllers();

// Health check endpoint
app.MapHealthChecks("/health");

// Simple health check with details
app.MapGet("/health", () => new
{
    status = "ok",
    service = "admin-bff",
    port = 3200,
    timestamp = DateTime.UtcNow
});

// Override port to 3200
var port = builder.Configuration["PORT"] ?? "3200";
app.Urls.Clear();
app.Urls.Add($"http://*:{port}");

logger.LogInformation("Starting Admin BFF on http://*:{Port}", port);

app.Run();
