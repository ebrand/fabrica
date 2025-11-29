namespace AdminBFF.Models;

public class SyncUserRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string StytchUserId { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
}

public class SyncUserResponseDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? StytchUserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public Guid? AvatarMediaId { get; set; }
    public bool IsSystemAdmin { get; set; }
    public bool IsNewUser { get; set; }
    public bool RequiresOnboarding { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public List<TenantAccessDto> Tenants { get; set; } = new();
}
