using Consul;
using System.Text.Json;

namespace AdminDomainService.Services;

/// <summary>
/// Service for reading service discovery information from Consul
/// </summary>
public class ConsulService
{
    private readonly IConsulClient _consulClient;
    private readonly ILogger<ConsulService> _logger;

    public ConsulService(IConfiguration configuration, ILogger<ConsulService> logger)
    {
        _logger = logger;

        var consulHost = configuration["Consul:Host"] ?? "consul";
        var consulPort = int.Parse(configuration["Consul:Port"] ?? "8500");
        var consulUrl = $"http://{consulHost}:{consulPort}";

        _consulClient = new ConsulClient(config =>
        {
            config.Address = new Uri(consulUrl);
        });

        _logger.LogInformation("ConsulService initialized. Consul Address: {ConsulUrl}", consulUrl);
    }

    /// <summary>
    /// Get service URL from Consul KV store
    /// </summary>
    public async Task<string> GetServiceUrlAsync(string serviceType, string serviceName)
    {
        try
        {
            var key = $"fabrica/ports/{serviceType}/{serviceName}";
            _logger.LogInformation("Fetching service URL from Consul: {Key}", key);

            var result = await _consulClient.KV.Get(key);

            if (result.Response == null)
            {
                _logger.LogWarning("Service not found in Consul: {Key}", key);
                return string.Empty;
            }

            var json = System.Text.Encoding.UTF8.GetString(result.Response.Value);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var serviceInfo = JsonSerializer.Deserialize<ServiceInfo>(json, options);

            if (serviceInfo == null)
            {
                _logger.LogWarning("Failed to parse service info for: {Key}", key);
                return string.Empty;
            }

            // Map service name to Docker container hostname for container-to-container communication
            var dockerHost = GetDockerHostname(serviceName);
            var url = $"http://{dockerHost}:{serviceInfo.Port}";
            _logger.LogInformation("Retrieved service URL: {ServiceName} -> {Url}", serviceName, url);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch service URL from Consul: {ServiceType}/{ServiceName}", serviceType, serviceName);
            return string.Empty;
        }
    }

    /// <summary>
    /// Get just the port number for a service (for browser/localhost URLs)
    /// </summary>
    public async Task<int> GetServicePortAsync(string serviceType, string serviceName)
    {
        var key = $"fabrica/ports/{serviceType}/{serviceName}";
        _logger.LogInformation("Fetching service port from Consul: {Key}", key);

        var result = await _consulClient.KV.Get(key);

        if (result.Response == null)
        {
            throw new Exception($"Service not found in Consul: {key}");
        }

        var json = System.Text.Encoding.UTF8.GetString(result.Response.Value);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var serviceInfo = JsonSerializer.Deserialize<ServiceInfo>(json, options);

        if (serviceInfo == null)
        {
            throw new Exception($"Failed to parse service info for: {key}");
        }

        return serviceInfo.Port;
    }

    /// <summary>
    /// Map service names to Docker container hostnames
    /// </summary>
    private static string GetDockerHostname(string serviceName)
    {
        return serviceName switch
        {
            "auth-iam" => "acl-admin",
            "admin-bff" => "bff-admin",
            "customer-api" => "acl-customer",
            "product-api" => "acl-product",
            "orders-api" => "acl-order",
            "storefront-bff" => "bff-storefront",
            "partner-bff" => "bff-partner",
            "mobile-bff" => "bff-mobile",
            "admin-mfe" => "mfe-admin",
            "catalog-mfe" => "mfe-product",
            "shell-admin" => "shell-admin",
            "storefront-shell" => "shell-storefront",
            // Infrastructure services use their service names as hostnames
            "postgres" or "redis" or "rabbitmq" or "consul" or "vault" => serviceName,
            // Default: use the service name as hostname (for services where names match)
            _ => serviceName
        };
    }

    /// <summary>
    /// Get all services of a specific type from Consul
    /// </summary>
    public async Task<Dictionary<string, string>> GetServicesByTypeAsync(string serviceType)
    {
        try
        {
            var prefix = $"fabrica/ports/{serviceType}/";
            _logger.LogInformation("Fetching services from Consul with prefix: {Prefix}", prefix);

            var result = await _consulClient.KV.List(prefix);

            if (result.Response == null || result.Response.Length == 0)
            {
                _logger.LogWarning("No services found in Consul for type: {ServiceType}", serviceType);
                return new Dictionary<string, string>();
            }

            var services = new Dictionary<string, string>();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var kvPair in result.Response)
            {
                var json = System.Text.Encoding.UTF8.GetString(kvPair.Value);
                var serviceInfo = JsonSerializer.Deserialize<ServiceInfo>(json, options);

                if (serviceInfo != null)
                {
                    var serviceName = serviceInfo.Service;
                    var dockerHost = GetDockerHostname(serviceName);
                    var url = $"http://{dockerHost}:{serviceInfo.Port}";
                    services[serviceName] = url;
                }
            }

            _logger.LogInformation("Retrieved {Count} services for type: {ServiceType}", services.Count, serviceType);
            return services;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch services from Consul for type: {ServiceType}", serviceType);
            return new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Helper class for deserializing Consul service info
    /// </summary>
    private class ServiceInfo
    {
        public int Port { get; set; }
        public string Service { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
