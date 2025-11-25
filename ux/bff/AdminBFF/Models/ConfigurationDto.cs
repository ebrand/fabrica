namespace AdminBFF.Models;

/// <summary>
/// Configuration from acl-configuration service
/// </summary>
public class BffConfiguration
{
    public DatabaseConfig Database { get; set; } = new();
    public ServiceUrls Services { get; set; } = new();
}

public class DatabaseConfig
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Database { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ServiceUrls
{
    public string AdminServiceUrl { get; set; } = string.Empty;
    public string CustomerServiceUrl { get; set; } = string.Empty;
    public string ProductServiceUrl { get; set; } = string.Empty;
    public string OrderServiceUrl { get; set; } = string.Empty;
}
