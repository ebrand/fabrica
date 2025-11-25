using AdminBFF.Models;

namespace AdminBFF.Services;

/// <summary>
/// Domain services registry
/// </summary>
public class ServicesRegistry
{
    private readonly IConfiguration _configuration;

    public ServicesRegistry(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Get all registered services
    /// </summary>
    public List<ServiceDto> GetServiceRegistry()
    {
        return new List<ServiceDto>
        {
            new ServiceDto
            {
                Id = "admin",
                Name = "Admin Domain Service",
                Description = "User management, authentication, and authorization",
                Port = 3600
            },
            new ServiceDto
            {
                Id = "product",
                Name = "Product Domain Service",
                Description = "Product catalog, categories, and inventory management",
                Port = 3420
            },
            new ServiceDto
            {
                Id = "content",
                Name = "Content Domain Service",
                Description = "CMS content, media, menus, and multi-language translations",
                Port = 3460
            }
        };
    }

    /// <summary>
    /// Get service configuration by ID
    /// </summary>
    public ServiceConfigDto? GetServiceById(string serviceId)
    {
        var services = new Dictionary<string, ServiceConfigDto>
        {
            ["admin"] = new ServiceConfigDto
            {
                Id = "admin",
                Name = "Admin Domain Service",
                Description = "User management, authentication, and authorization",
                BaseUrl = _configuration["ADMIN_SERVICE_URL"] ?? "http://acl-admin:3600",
                SwaggerPath = "/swagger/v1/swagger.json",
                Port = 3600
            },
            ["product"] = new ServiceConfigDto
            {
                Id = "product",
                Name = "Product Domain Service",
                Description = "Product catalog, categories, and inventory management",
                BaseUrl = _configuration["PRODUCT_SERVICE_URL"] ?? "http://acl-product:3420",
                SwaggerPath = "/swagger/v1/swagger.json",
                Port = 3420
            },
            ["content"] = new ServiceConfigDto
            {
                Id = "content",
                Name = "Content Domain Service",
                Description = "CMS content, media, menus, and multi-language translations",
                BaseUrl = _configuration["CONTENT_SERVICE_URL"] ?? "http://acl-content:3460",
                SwaggerPath = "/swagger/v1/swagger.json",
                Port = 3460
            }
        };

        return services.TryGetValue(serviceId, out var service) ? service : null;
    }
}
