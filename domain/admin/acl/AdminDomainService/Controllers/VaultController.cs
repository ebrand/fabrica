using AdminDomainService.Models;
using AdminDomainService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdminDomainService.Controllers;

[ApiController]
[Route("api/vault")]
public class VaultController : ControllerBase
{
    private readonly VaultService _vaultService;
    private readonly ConsulService _consulService;
    private readonly ILogger<VaultController> _logger;

    public VaultController(VaultService vaultService, ConsulService consulService, ILogger<VaultController> logger)
    {
        _vaultService = vaultService;
        _consulService = consulService;
        _logger = logger;
    }

    /// <summary>
    /// Get authentication configuration (Stytch, Google OAuth)
    /// </summary>
    [HttpGet("auth")]
    public async Task<ActionResult<AuthConfig>> GetAuthConfig()
    {
        try
        {
            _logger.LogInformation("Fetching auth configuration from Vault");

            var stytchData = await _vaultService.GetSecretDataAsync("admin/stytch");
            var googleData = await _vaultService.GetSecretDataAsync("admin/oauth/google");

            var config = new AuthConfig
            {
                Stytch = new StytchConfig
                {
                    PublicToken = stytchData["public_token"]?.ToString() ?? string.Empty,
                    ProjectDomain = stytchData["project_domain"]?.ToString() ?? string.Empty
                },
                Google = new GoogleConfig
                {
                    ClientId = googleData["client_id"]?.ToString() ?? string.Empty,
                    RedirectUri = googleData["redirect_uri"]?.ToString() ?? string.Empty
                }
            };

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch auth configuration from Vault");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get BFF Admin configuration (database from Vault + service URLs from Consul)
    /// </summary>
    [HttpGet("bff-admin")]
    public async Task<ActionResult<BffConfig>> GetBffAdminConfig()
    {
        try
        {
            _logger.LogInformation("Fetching BFF Admin configuration from Vault and Consul");

            // Get database secrets from Vault
            var dbData = await _vaultService.GetSecretDataAsync("infrastructure/postgres");

            // Get service URLs from Consul (fallbacks use Docker network hostnames for container-to-container communication)
            var adminServiceUrl = await _consulService.GetServiceUrlAsync("shared", "auth-iam");
            if (string.IsNullOrEmpty(adminServiceUrl))
                adminServiceUrl = "http://acl-admin:3600";

            var bffAdminUrl = await _consulService.GetServiceUrlAsync("bff", "admin-bff");
            if (string.IsNullOrEmpty(bffAdminUrl))
                bffAdminUrl = "http://bff-admin:3200";

            var customerServiceUrl = await _consulService.GetServiceUrlAsync("domain", "customer-api");
            if (string.IsNullOrEmpty(customerServiceUrl))
                customerServiceUrl = "http://acl-customer:3410";

            var productServiceUrl = await _consulService.GetServiceUrlAsync("domain", "product-api");
            if (string.IsNullOrEmpty(productServiceUrl))
                productServiceUrl = "http://acl-product:3420";

            var orderServiceUrl = await _consulService.GetServiceUrlAsync("domain", "orders-api");
            if (string.IsNullOrEmpty(orderServiceUrl))
                orderServiceUrl = "http://acl-order:3430";

            var config = new BffConfig
            {
                Database = new DatabaseConfig
                {
                    Host     = dbData["host"]?.ToString() ?? "postgres",
                    Port     = dbData["port"]?.ToString() ?? "5432",
                    Database = dbData["database"]?.ToString() ?? "fabrica-admin-db",
                    User     = dbData["user"]?.ToString() ?? "fabrica_admin",
                    Password = dbData["password"]?.ToString() ?? string.Empty
                },
                Services = new ServicesConfig
                {
                    AdminServiceUrl    = adminServiceUrl,
                    BffAdminUrl        = bffAdminUrl,
                    CustomerServiceUrl = customerServiceUrl,
                    ProductServiceUrl  = productServiceUrl,
                    OrderServiceUrl    = orderServiceUrl
                }
            };

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch BFF Admin configuration");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get ACL Admin domain service configuration (.NET format)
    /// </summary>
    [HttpGet("acl-admin")]
    public async Task<ActionResult<AclAdminConfig>> GetAclAdminConfig()
    {
        try
        {
            _logger.LogInformation("Fetching ACL Admin configuration from Vault");

            var dbData       = await _vaultService.GetSecretDataAsync("infrastructure/postgres");
            var rabbitmqData = await _vaultService.GetSecretDataAsync("infrastructure/rabbitmq");
            var redisData    = await _vaultService.GetSecretDataAsync("infrastructure/redis");
            var consulData   = await _vaultService.GetSecretDataAsync("shared/consul");

            var config = new AclAdminConfig
            {
                ConnectionStrings = new ConnectionStrings
                {
                    DefaultConnection = $"Host={dbData["host"]};Port={dbData["port"]};Database={dbData["database"]};Username={dbData["user"]};Password={dbData["password"]}"
                },
                RabbitMQ = new RabbitMQConfig
                {
                    Host     = rabbitmqData["host"]?.ToString() ?? "rabbitmq",
                    Port     = int.TryParse(rabbitmqData["port"]?.ToString(), out var rabbitPort) ? rabbitPort : 5672,
                    Username = rabbitmqData["username"]?.ToString() ?? "fabrica_admin",
                    Password = rabbitmqData["password"]?.ToString() ?? string.Empty
                },
                Redis = new RedisConfig
                {
                    ConnectionString = $"{redisData["host"] ?? "redis"}:{redisData["port"] ?? "6379"}"
                },
                Consul = new ConsulConfig
                {
                    Host = consulData["host"]?.ToString() ?? "consul",
                    Port = int.TryParse(consulData["port"]?.ToString(), out var consulPort) ? consulPort : 8500
                }
            };

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch ACL Admin configuration from Vault");
            return BadRequest(new { error = ex.Message });
        }
    }
}
