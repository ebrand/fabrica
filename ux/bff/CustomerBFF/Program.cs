using CustomerBFF.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Customer BFF API", Version = "v1" });
});

// Add HttpContextAccessor for tenant context propagation
builder.Services.AddHttpContextAccessor();

// Add HttpClient for CustomerServiceClient
builder.Services.AddHttpClient<CustomerBFF.Services.CustomerServiceClient>();

// Add CORS - allow credentials for cookie-based tenant context
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

var app = builder.Build();

// Configure pipeline
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();

// Use tenant middleware to extract tenant context from cookies/headers
app.UseTenantMiddleware();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "bff-customer",
    port = 3250,
    timestamp = DateTime.UtcNow
}));

app.MapControllers();

app.Run();
