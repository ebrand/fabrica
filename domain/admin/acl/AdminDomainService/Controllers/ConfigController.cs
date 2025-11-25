using AdminDomainService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdminDomainService.Controllers;

[ApiController]
[Route("api/config")]
public class ConfigController : ControllerBase
{
    private readonly ConsulService _consulService;
    private readonly VaultService _vaultService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigController> _logger;

    public ConfigController(
        ConsulService consulService,
        VaultService vaultService,
        IConfiguration configuration,
        ILogger<ConfigController> logger)
    {
        _consulService = consulService;
        _vaultService = vaultService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Get service URLs for frontend/browser clients (no secrets)
    /// These URLs use localhost because browsers run on the host machine, not in Docker
    /// </summary>
    [HttpGet("services")]
    public async Task<ActionResult> GetServiceUrls()
    {
        try
        {
            _logger.LogInformation("Fetching service URLs from Consul for frontend (browser) clients");

            // Get port info from Consul, but use localhost for browser access
            var bffAdminUrl = await GetBrowserUrlAsync("bff", "admin-bff", 3200);
            var adminMfeUrl = await GetBrowserUrlAsync("mfe", "admin-mfe", 3100);
            var productMfeUrl = await GetBrowserUrlAsync("mfe", "catalog-mfe", 3110);
            var bffProductUrl = await GetBrowserUrlAsync("bff", "product-bff", 3220);

            var config = new
            {
                bffAdminUrl,
                bffProductUrl,
                adminMfeUrl,
                productMfeUrl,
                aclAdminUrl = "http://localhost:3600" // Self-reference for completeness
            };

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch service URLs from Consul");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get a localhost URL for browser clients by looking up port in Consul
    /// </summary>
    private async Task<string> GetBrowserUrlAsync(string serviceType, string serviceName, int fallbackPort)
    {
        try
        {
            var port = await _consulService.GetServicePortAsync(serviceType, serviceName);
            return $"http://localhost:{port}";
        }
        catch
        {
            return $"http://localhost:{fallbackPort}";
        }
    }

    /// <summary>
    /// Get database connection string for a specific domain
    /// Internal service-to-service endpoint
    /// </summary>
    [HttpGet("database/{domain}")]
    public async Task<ActionResult> GetDatabaseConfig(string domain)
    {
        try
        {
            _logger.LogInformation("Fetching database configuration for domain: {Domain}", domain);

            string connectionString;

            // Try to get from Vault first
            try
            {
                var secretData = await _vaultService.GetSecretDataAsync($"database/{domain}");
                if (secretData.TryGetValue("connection_string", out var vaultConnString))
                {
                    connectionString = vaultConnString.ToString() ?? string.Empty;
                    _logger.LogInformation("Retrieved connection string from Vault for domain: {Domain}", domain);
                }
                else
                {
                    throw new Exception("Connection string not found in Vault");
                }
            }
            catch (Exception vaultEx)
            {
                _logger.LogWarning(vaultEx, "Failed to retrieve from Vault, using fallback configuration for domain: {Domain}", domain);

                // Fallback to environment variables / configuration
                connectionString = domain.ToLower() switch
                {
                    "product" => _configuration.GetConnectionString("ProductDb")
                        ?? "Host=postgres;Port=5432;Database=fabrica-product-db;Username=fabrica_admin;Password=fabrica_dev_password",
                    "admin" => _configuration.GetConnectionString("DefaultConnection")
                        ?? "Host=postgres;Port=5432;Database=fabrica-admin-db;Username=fabrica_admin;Password=fabrica_dev_password",
                    "customer" => _configuration.GetConnectionString("CustomerDb")
                        ?? "Host=postgres;Port=5432;Database=fabrica-customer-db;Username=fabrica_admin;Password=fabrica_dev_password",
                    "order" => _configuration.GetConnectionString("OrderDb")
                        ?? "Host=postgres;Port=5432;Database=fabrica-order-db;Username=fabrica_admin;Password=fabrica_dev_password",
                    "content" => _configuration.GetConnectionString("ContentDb")
                        ?? "Host=postgres;Port=5432;Database=fabrica-content-db;Username=fabrica_admin;Password=fabrica_dev_password",
                    _ => throw new Exception($"Unknown domain: {domain}")
                };
            }

            var config = new
            {
                domain,
                connectionString
            };

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch database configuration for domain: {Domain}", domain);
            return BadRequest(new { error = ex.Message });
        }
    }
}
