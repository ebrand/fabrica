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
            _logger.LogInformation("Fetching service URLs for frontend (browser) clients");

            // Try to get from Vault first, fall back to Consul/defaults
            var browserUrls = await GetBrowserUrlsFromVaultAsync();

            var config = new
            {
                bffAdminUrl = browserUrls.GetValueOrDefault("bffAdmin", "http://localhost:3200"),
                bffProductUrl = browserUrls.GetValueOrDefault("bffProduct", "http://localhost:3220"),
                bffContentUrl = browserUrls.GetValueOrDefault("bffContent", "http://localhost:3240"),
                bffCustomerUrl = browserUrls.GetValueOrDefault("bffCustomer", "http://localhost:3250"),
                adminMfeUrl = browserUrls.GetValueOrDefault("mfeAdmin", "http://localhost:3100"),
                productMfeUrl = browserUrls.GetValueOrDefault("mfeProduct", "http://localhost:3110"),
                contentMfeUrl = browserUrls.GetValueOrDefault("mfeContent", "http://localhost:3180"),
                customerMfeUrl = browserUrls.GetValueOrDefault("mfeCustomer", "http://localhost:3170"),
                commonMfeUrl = browserUrls.GetValueOrDefault("mfeCommon", "http://localhost:3099"),
                aclAdminUrl = "http://localhost:3600"
            };

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch service URLs");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get service URLs for container-to-container communication
    /// Internal endpoint for other services
    /// </summary>
    [HttpGet("services/internal")]
    public async Task<ActionResult> GetInternalServiceUrls()
    {
        try
        {
            _logger.LogInformation("Fetching internal service URLs for container-to-container communication");

            var internalUrls = await GetInternalUrlsFromVaultAsync();

            var config = new
            {
                aclAdmin = internalUrls.GetValueOrDefault("aclAdmin", "http://acl-admin:3600"),
                aclProduct = internalUrls.GetValueOrDefault("aclProduct", "http://acl-product:3420"),
                aclContent = internalUrls.GetValueOrDefault("aclContent", "http://acl-content:3460"),
                aclCustomer = internalUrls.GetValueOrDefault("aclCustomer", "http://acl-customer:3410"),
                aclOrder = internalUrls.GetValueOrDefault("aclOrder", "http://acl-order:3430"),
                bffAdmin = internalUrls.GetValueOrDefault("bffAdmin", "http://bff-admin:3200"),
                bffProduct = internalUrls.GetValueOrDefault("bffProduct", "http://bff-product:3220"),
                bffContent = internalUrls.GetValueOrDefault("bffContent", "http://bff-content:3240"),
                bffCustomer = internalUrls.GetValueOrDefault("bffCustomer", "http://bff-customer:3250")
            };

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch internal service URLs");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Helper to safely get string value from IDictionary
    /// </summary>
    private static string GetStringValue(IDictionary<string, object> dict, string key, string defaultValue)
    {
        if (dict.TryGetValue(key, out var value) && value != null)
            return value.ToString() ?? defaultValue;
        return defaultValue;
    }

    /// <summary>
    /// Get browser URLs from Vault
    /// </summary>
    private async Task<Dictionary<string, string>> GetBrowserUrlsFromVaultAsync()
    {
        var urls = new Dictionary<string, string>();

        try
        {
            var bffUrls = await _vaultService.GetSecretDataAsync("services/browser/bff");
            urls["bffAdmin"] = GetStringValue(bffUrls, "admin", "http://localhost:3200");
            urls["bffProduct"] = GetStringValue(bffUrls, "product", "http://localhost:3220");
            urls["bffContent"] = GetStringValue(bffUrls, "content", "http://localhost:3240");
            urls["bffCustomer"] = GetStringValue(bffUrls, "customer", "http://localhost:3250");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get BFF browser URLs from Vault, using defaults");
        }

        try
        {
            var mfeUrls = await _vaultService.GetSecretDataAsync("services/browser/mfe");
            urls["mfeAdmin"] = GetStringValue(mfeUrls, "admin", "http://localhost:3100");
            urls["mfeProduct"] = GetStringValue(mfeUrls, "product", "http://localhost:3110");
            urls["mfeContent"] = GetStringValue(mfeUrls, "content", "http://localhost:3180");
            urls["mfeCustomer"] = GetStringValue(mfeUrls, "customer", "http://localhost:3170");
            urls["mfeCommon"] = GetStringValue(mfeUrls, "common", "http://localhost:3099");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get MFE browser URLs from Vault, using defaults");
        }

        return urls;
    }

    /// <summary>
    /// Get internal (container-to-container) URLs from Vault
    /// </summary>
    private async Task<Dictionary<string, string>> GetInternalUrlsFromVaultAsync()
    {
        var urls = new Dictionary<string, string>();

        try
        {
            var aclUrls = await _vaultService.GetSecretDataAsync("services/acl");
            urls["aclAdmin"] = GetStringValue(aclUrls, "admin", "http://acl-admin:3600");
            urls["aclProduct"] = GetStringValue(aclUrls, "product", "http://acl-product:3420");
            urls["aclContent"] = GetStringValue(aclUrls, "content", "http://acl-content:3460");
            urls["aclCustomer"] = GetStringValue(aclUrls, "customer", "http://acl-customer:3410");
            urls["aclOrder"] = GetStringValue(aclUrls, "order", "http://acl-order:3430");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get ACL URLs from Vault, using defaults");
        }

        try
        {
            var bffUrls = await _vaultService.GetSecretDataAsync("services/bff");
            urls["bffAdmin"] = GetStringValue(bffUrls, "admin", "http://bff-admin:3200");
            urls["bffProduct"] = GetStringValue(bffUrls, "product", "http://bff-product:3220");
            urls["bffContent"] = GetStringValue(bffUrls, "content", "http://bff-content:3240");
            urls["bffCustomer"] = GetStringValue(bffUrls, "customer", "http://bff-customer:3250");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get BFF URLs from Vault, using defaults");
        }

        return urls;
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

            // Try to get from Vault first using correct path: {domain}/database
            try
            {
                var secretData = await _vaultService.GetSecretDataAsync($"{domain}/database");
                var sharedDb = await _vaultService.GetSecretDataAsync("shared/database");

                // Build connection string from Vault data
                var dbName = GetStringValue(secretData, "name", $"fabrica-{domain}-db");
                var username = GetStringValue(secretData, "username", "fabrica_admin");
                var password = GetStringValue(secretData, "password", "fabrica_dev_password");
                var host = GetStringValue(sharedDb, "host", "postgres");
                var port = GetStringValue(sharedDb, "port", "5432");

                connectionString = $"Host={host};Port={port};Database={dbName};Username={username};Password={password}";
                _logger.LogInformation("Built connection string from Vault for domain: {Domain}", domain);
            }
            catch (Exception vaultEx)
            {
                _logger.LogWarning(vaultEx, "Failed to retrieve from Vault, using fallback for domain: {Domain}", domain);

                // Fallback: build connection string using Docker hostname
                var dbName = $"fabrica-{domain}-db";
                connectionString = $"Host=postgres;Port=5432;Database={dbName};Username=fabrica_admin;Password=fabrica_dev_password";
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
