namespace AdminBFF.Models;

/// <summary>
/// Service registry DTO
/// </summary>
public class ServiceDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Port { get; set; }
}

/// <summary>
/// Full service configuration
/// </summary>
public class ServiceConfigDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public string SwaggerPath { get; set; } = string.Empty;
    public int Port { get; set; }
}
