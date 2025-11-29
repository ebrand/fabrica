using AdminBFF.Models;
using System.Text.Json;

namespace AdminBFF.Services;

/// <summary>
/// Configuration Service - Fetches configuration from acl-configuration service
/// </summary>
public class ConfigurationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ConfigurationService> _logger;
    private readonly IConfiguration _configuration;
    private BffConfiguration? _cachedConfig;

    public ConfigurationService(
        IHttpClientFactory httpClientFactory,
        ILogger<ConfigurationService> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Fetch BFF configuration from acl-configuration service
    /// </summary>
    public async Task<BffConfiguration> FetchConfigAsync()
    {
        if (_cachedConfig != null)
        {
            return _cachedConfig;
        }

        try
        {
            _logger.LogInformation("Fetching configuration from acl-admin Vault service...");

            var adminServiceUrl = _configuration["ADMIN_SERVICE_URL"] ?? "http://acl-admin:3600";
            var httpClient = _httpClientFactory.CreateClient();

            var response = await httpClient.GetAsync($"{adminServiceUrl}/api/vault/bff-admin");
            response.EnsureSuccessStatusCode();

            var config = await response.Content.ReadFromJsonAsync<BffConfiguration>();

            if (config != null)
            {
                _cachedConfig = config;
                _logger.LogInformation("Configuration loaded from acl-admin Vault service");
                _logger.LogInformation("Admin Service URL: {AdminServiceUrl}", config.Services.AdminServiceUrl);
                return config;
            }
            else
            {
                _logger.LogWarning("Configuration response was null, falling back to environment variables");
                return GetFallbackConfiguration();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load config from acl-admin Vault service");
            _logger.LogWarning("Falling back to environment variables");
            return GetFallbackConfiguration();
        }
    }

    /// <summary>
    /// Get database configuration
    /// </summary>
    public async Task<DatabaseConfig> GetDatabaseConfigAsync()
    {
        var config = await FetchConfigAsync();
        return config.Database;
    }

    /// <summary>
    /// Get service URLs
    /// </summary>
    public async Task<ServiceUrls> GetServiceUrlsAsync()
    {
        var config = await FetchConfigAsync();
        return config.Services;
    }

    /// <summary>
    /// Get specific service URL
    /// </summary>
    public async Task<string> GetServiceUrlAsync(string serviceName)
    {
        var services = await GetServiceUrlsAsync();
        return serviceName.ToLower() switch
        {
            "admin" => services.AdminServiceUrl,
            "customer" => services.CustomerServiceUrl,
            "product" => services.ProductServiceUrl,
            "order" => services.OrderServiceUrl,
            "content" => services.ContentServiceUrl,
            _ => throw new ArgumentException($"Unknown service: {serviceName}", nameof(serviceName))
        };
    }

    /// <summary>
    /// Fallback configuration from environment variables
    /// </summary>
    private BffConfiguration GetFallbackConfiguration()
    {
        _cachedConfig = new BffConfiguration
        {
            Database = new DatabaseConfig
            {
                Host = _configuration["POSTGRES_HOST"] ?? "postgres",
                Port = int.Parse(_configuration["POSTGRES_PORT"] ?? "5432"),
                Database = _configuration["POSTGRES_DB"] ?? "fabrica-admin-db",
                User = _configuration["POSTGRES_USER"] ?? "fabrica_admin",
                Password = _configuration["POSTGRES_PASSWORD"] ?? "fabrica_dev_password"
            },
            Services = new ServiceUrls
            {
                // All fallbacks use Docker container hostnames for container-to-container communication
                AdminServiceUrl = _configuration["ADMIN_SERVICE_URL"] ?? "http://acl-admin:3600",
                CustomerServiceUrl = "http://acl-customer:3410",
                ProductServiceUrl = "http://acl-product:3420",
                OrderServiceUrl = "http://acl-order:3430",
                ContentServiceUrl = _configuration["CONTENT_SERVICE_URL"] ?? "http://acl-content:3460"
            }
        };

        return _cachedConfig;
    }
}
