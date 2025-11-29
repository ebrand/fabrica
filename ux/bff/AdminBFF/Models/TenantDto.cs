namespace AdminBFF.Models;

public class TenantDto
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? LogoMediaId { get; set; }
    public bool IsActive { get; set; }
    public bool IsPersonal { get; set; }
    public Guid? OwnerUserId { get; set; }
    public string Settings { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TenantAccessDto
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsPersonal { get; set; }
}

public class CreateTenantRequest
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? LogoMediaId { get; set; }
    public Guid? OwnerUserId { get; set; }
    public Guid? CreatedBy { get; set; }
}

public class UpdateTenantRequest
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public Guid? LogoMediaId { get; set; }
    public bool? IsActive { get; set; }
    public string? Settings { get; set; }
    public Guid? UpdatedBy { get; set; }
}
