using AdminBFF.Services;
using AdminBFF.Hubs;
using AdminBFF.BackgroundServices;
using AdminBFF.Middleware;

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

// Add SignalR for real-time telemetry
builder.Services.AddSignalR();

// Add TelemetryConsumer background service
builder.Services.AddHostedService<TelemetryConsumer>();

// Add CORS - allow credentials for cookie-based auth/tenant context
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add IHttpClientFactory for all services
builder.Services.AddHttpClient();

// Add IHttpContextAccessor for accessing HTTP context in services
builder.Services.AddHttpContextAccessor();

// Register ConfigurationService as singleton
builder.Services.AddSingleton<ConfigurationService>();

// Add HTTP client for AdminServiceClient
builder.Services.AddHttpClient<AdminServiceClient>();

// Add AdminServiceClient as scoped
builder.Services.AddScoped<AdminServiceClient>();

// Add HTTP client for ContentServiceClient
builder.Services.AddHttpClient<ContentServiceClient>();

// Add ContentServiceClient as scoped (for cross-domain operations like language provisioning)
builder.Services.AddScoped<ContentServiceClient>();

// Add ServicesRegistry
builder.Services.AddSingleton<ServicesRegistry>();

// Add StripeService for payment processing
builder.Services.AddSingleton<StripeService>();

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

// Use tenant middleware to extract tenant context from cookies/headers
app.UseTenantMiddleware();

// Map controllers
app.MapControllers();

// Map SignalR hub (uses default CORS policy which allows credentials)
app.MapHub<TelemetryHub>("/hubs/telemetry");

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
