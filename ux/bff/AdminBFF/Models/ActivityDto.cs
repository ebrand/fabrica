namespace AdminBFF.Models;

/// <summary>
/// Activity log DTO
/// </summary>
public class ActivityDto
{
    public string Id { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
