using ContentBFF.Services;
using ContentBFF.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Content BFF API",
        Version = "v1",
        Description = "Backend-for-Frontend aggregating content data from the Content domain service"
    });
});

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

// Add HTTP client for ContentServiceClient
builder.Services.AddHttpClient<ContentServiceClient>();

// Add ContentServiceClient as scoped
builder.Services.AddScoped<ContentServiceClient>();

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Content BFF API v1");
    });
}

// Use CORS
app.UseCors();

// Use tenant middleware to extract tenant context from cookies/headers
app.UseTenantMiddleware();

// Map controllers
app.MapControllers();

// Health check endpoint
app.MapHealthChecks("/health");

// Simple health check with details
app.MapGet("/health", () => new
{
    status = "healthy",
    service = "bff-content",
    port = 3240,
    timestamp = DateTime.UtcNow
});

// Override port to 3240
var port = builder.Configuration["PORT"] ?? "3240";
app.Urls.Clear();
app.Urls.Add($"http://*:{port}");

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting Content BFF on http://*:{Port}", port);

app.Run();
