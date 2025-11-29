using AdminDomainService.Data;
using AdminDomainService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdminDomainService.Controllers;

[ApiController]
[Route("api/invitation")]
public class InvitationController : ControllerBase
{
    private readonly AdminDbContext _context;
    private readonly ILogger<InvitationController> _logger;
    private const int InvitationExpirationDays = 7;

    public InvitationController(AdminDbContext context, ILogger<InvitationController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Checks if the caller is a System Admin via the X-Is-System-Admin header
    /// </summary>
    private bool IsCallerSystemAdmin()
    {
        return Request.Headers.TryGetValue("X-Is-System-Admin", out var value)
            && value.FirstOrDefault()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Gets the tenant ID from the X-Tenant-ID header
    /// </summary>
    private Guid? GetTenantIdFromHeader()
    {
        if (Request.Headers.TryGetValue("X-Tenant-ID", out var value))
        {
            var tenantIdString = value.FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantIdString) && Guid.TryParse(tenantIdString, out var tenantId))
            {
                if (tenantId == Guid.Empty)
                {
                    return null;
                }
                return tenantId;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets the caller's user ID from the X-User-ID header
    /// </summary>
    private Guid? GetCallerUserId()
    {
        if (Request.Headers.TryGetValue("X-User-ID", out var value))
        {
            var userIdString = value.FirstOrDefault();
            if (!string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out var userId))
            {
                return userId;
            }
        }
        return null;
    }

    /// <summary>
    /// Checks if the caller is the owner of the specified tenant
    /// </summary>
    private async Task<bool> IsCallerTenantOwnerAsync(Guid tenantId)
    {
        var callerId = GetCallerUserId();
        if (!callerId.HasValue)
        {
            return false;
        }

        var userTenant = await _context.UserTenants
            .FirstOrDefaultAsync(ut => ut.UserId == callerId.Value
                && ut.TenantId == tenantId
                && ut.IsActive);

        return userTenant?.Role == "owner";
    }

    /// <summary>
    /// Checks if the caller can manage invitations in the current tenant context
    /// Returns true if: System Admin, or tenant owner
    /// </summary>
    private async Task<bool> CanCallerManageInvitationsAsync()
    {
        if (IsCallerSystemAdmin())
        {
            return true;
        }

        var tenantId = GetTenantIdFromHeader();
        if (tenantId.HasValue)
        {
            return await IsCallerTenantOwnerAsync(tenantId.Value);
        }

        return false;
    }

    /// <summary>
    /// Get all pending invitations for current tenant
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<InvitationResponseDto>>> GetInvitations()
    {
        try
        {
            var tenantId = GetTenantIdFromHeader();

            if (!tenantId.HasValue)
            {
                return BadRequest(new { error = "Tenant context required. Please select a tenant." });
            }

            // Verify caller can view invitations
            if (!await CanCallerManageInvitationsAsync())
            {
                return StatusCode(403, new { error = "You do not have permission to view invitations" });
            }

            var invitations = await _context.Invitations
                .Where(i => i.TenantId == tenantId.Value && i.Status == "pending")
                .Include(i => i.Tenant)
                .Include(i => i.InvitedByUser)
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new InvitationResponseDto
                {
                    InvitationId = i.InvitationId,
                    Email = i.Email,
                    TenantId = i.TenantId,
                    TenantName = i.Tenant != null ? i.Tenant.Name : "",
                    Status = i.Status,
                    ExpiresAt = i.ExpiresAt,
                    InvitedByName = i.InvitedByUser != null
                        ? i.InvitedByUser.DisplayName ?? $"{i.InvitedByUser.FirstName} {i.InvitedByUser.LastName}".Trim()
                        : "",
                    CreatedAt = i.CreatedAt
                })
                .ToListAsync();

            return Ok(invitations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching invitations");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Create a new invitation
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<InvitationResponseDto>> CreateInvitation([FromBody] CreateInvitationDto dto)
    {
        try
        {
            var tenantId = GetTenantIdFromHeader();
            var callerId = GetCallerUserId();

            if (!tenantId.HasValue)
            {
                return BadRequest(new { error = "Tenant context required. Please select a tenant." });
            }

            if (!callerId.HasValue)
            {
                return BadRequest(new { error = "User context required" });
            }

            // Authorization: Only tenant owner or system admin can create invitations
            if (!await CanCallerManageInvitationsAsync())
            {
                _logger.LogWarning("Unauthorized invitation creation attempt by {CallerId}", callerId);
                return StatusCode(403, new { error = "You do not have permission to invite users to this tenant" });
            }

            // Validate email format
            if (string.IsNullOrWhiteSpace(dto.Email) || !dto.Email.Contains("@"))
            {
                return BadRequest(new { error = "Valid email address is required" });
            }

            var normalizedEmail = dto.Email.Trim().ToLower();

            // Check if user is trying to invite themselves
            var caller = await _context.Users.FindAsync(callerId.Value);
            if (caller != null && caller.Email.ToLower() == normalizedEmail)
            {
                return BadRequest(new { error = "You cannot invite yourself" });
            }

            // Check if there's already a pending invitation for this email+tenant
            var existingInvitation = await _context.Invitations
                .FirstOrDefaultAsync(i => i.Email.ToLower() == normalizedEmail
                    && i.TenantId == tenantId.Value
                    && i.Status == "pending");

            if (existingInvitation != null)
            {
                return Conflict(new { error = "An invitation has already been sent to this email address" });
            }

            // Check if user is already a member of this tenant
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            if (existingUser != null)
            {
                var existingMembership = await _context.UserTenants
                    .FirstOrDefaultAsync(ut => ut.UserId == existingUser.UserId
                        && ut.TenantId == tenantId.Value
                        && ut.IsActive);

                if (existingMembership != null)
                {
                    return Conflict(new { error = "This user is already a member of this tenant" });
                }
            }

            // Create the invitation
            var invitation = new Invitation
            {
                InvitationId = Guid.NewGuid(),
                Email = normalizedEmail,
                TenantId = tenantId.Value,
                InvitedBy = callerId.Value,
                Status = "pending",
                ExpiresAt = DateTime.UtcNow.AddDays(InvitationExpirationDays),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Invitations.Add(invitation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Invitation created: {Email} invited to tenant {TenantId} by {InvitedBy}",
                normalizedEmail, tenantId.Value, callerId.Value);

            // Load navigation properties for response
            await _context.Entry(invitation).Reference(i => i.Tenant).LoadAsync();
            await _context.Entry(invitation).Reference(i => i.InvitedByUser).LoadAsync();

            var response = new InvitationResponseDto
            {
                InvitationId = invitation.InvitationId,
                Email = invitation.Email,
                TenantId = invitation.TenantId,
                TenantName = invitation.Tenant?.Name ?? "",
                Status = invitation.Status,
                ExpiresAt = invitation.ExpiresAt,
                InvitedByName = invitation.InvitedByUser != null
                    ? invitation.InvitedByUser.DisplayName ?? $"{invitation.InvitedByUser.FirstName} {invitation.InvitedByUser.LastName}".Trim()
                    : "",
                CreatedAt = invitation.CreatedAt
            };

            return CreatedAtAction(nameof(GetInvitations), response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invitation");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Revoke an invitation
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> RevokeInvitation(Guid id)
    {
        try
        {
            var tenantId = GetTenantIdFromHeader();

            if (!tenantId.HasValue)
            {
                return BadRequest(new { error = "Tenant context required. Please select a tenant." });
            }

            // Authorization: Only tenant owner or system admin can revoke invitations
            if (!await CanCallerManageInvitationsAsync())
            {
                _logger.LogWarning("Unauthorized invitation revocation attempt by {CallerId}", GetCallerUserId());
                return StatusCode(403, new { error = "You do not have permission to revoke invitations" });
            }

            var invitation = await _context.Invitations
                .FirstOrDefaultAsync(i => i.InvitationId == id && i.TenantId == tenantId.Value);

            if (invitation == null)
            {
                return NotFound(new { error = "Invitation not found" });
            }

            if (invitation.Status != "pending")
            {
                return BadRequest(new { error = $"Cannot revoke invitation with status '{invitation.Status}'" });
            }

            invitation.Status = "revoked";
            invitation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Invitation {InvitationId} revoked by {UserId}", id, GetCallerUserId());

            return Ok(new { message = "Invitation revoked successfully", id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking invitation {InvitationId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }
}
