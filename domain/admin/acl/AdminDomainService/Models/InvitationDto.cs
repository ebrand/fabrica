namespace AdminDomainService.Models;

/// <summary>
/// DTO for creating an invitation
/// </summary>
public class CreateInvitationDto
{
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// DTO for invitation response
/// </summary>
public class InvitationResponseDto
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
