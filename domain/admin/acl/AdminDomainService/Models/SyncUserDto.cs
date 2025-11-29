using System.ComponentModel.DataAnnotations;

namespace AdminDomainService.Models;

public class SyncUserDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
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

public class TenantAccessDto
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsPersonal { get; set; }
}
