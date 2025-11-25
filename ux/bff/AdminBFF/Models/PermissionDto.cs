namespace AdminBFF.Models;

/// <summary>
/// Permission DTO
/// </summary>
public class PermissionDto
{
    public string Id { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }
}
