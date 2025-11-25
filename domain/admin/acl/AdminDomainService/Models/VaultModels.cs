namespace AdminDomainService.Models;

// BFF Configuration Models
public class BffConfig
{
    public DatabaseConfig Database { get; set; } = new();
    public ServicesConfig Services { get; set; } = new();
}

public class DatabaseConfig
{
    public string Host { get; set; } = string.Empty;
    public string Port { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ServicesConfig
{
    public string AdminServiceUrl { get; set; } = string.Empty;
    public string BffAdminUrl { get; set; } = string.Empty;
    public string CustomerServiceUrl { get; set; } = string.Empty;
    public string ProductServiceUrl { get; set; } = string.Empty;
    public string OrderServiceUrl { get; set; } = string.Empty;
}

// ACL Admin Configuration Models
public class AclAdminConfig
{
    public ConnectionStrings ConnectionStrings { get; set; } = new();
    public RabbitMQConfig RabbitMQ { get; set; } = new();
    public RedisConfig Redis { get; set; } = new();
    public ConsulConfig Consul { get; set; } = new();
}

public class ConnectionStrings
{
    public string DefaultConnection { get; set; } = string.Empty;
}

public class RabbitMQConfig
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RedisConfig
{
    public string ConnectionString { get; set; } = string.Empty;
}

public class ConsulConfig
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
}

// Authentication Configuration Models
public class AuthConfig
{
    public StytchConfig Stytch { get; set; } = new();
    public GoogleConfig Google { get; set; } = new();
}

public class StytchConfig
{
    public string PublicToken { get; set; } = string.Empty;
    public string ProjectDomain { get; set; } = string.Empty;
}

public class GoogleConfig
{
    public string ClientId { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
}
