namespace AdminBFF.Models;

/// <summary>
/// Request DTO for creating an invitation
/// </summary>
public class CreateInvitationRequest
{
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for invitation data
/// </summary>
public class InvitationDto
{
    public Guid InvitationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string InvitedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response from ACL for invitation operations
/// </summary>
public class AclInvitationResponse
{
    public Guid InvitationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string InvitedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
