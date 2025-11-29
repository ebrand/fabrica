using AdminBFF.Models;
using AdminBFF.Services;
using AdminBFF.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace AdminBFF.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvitationsController : ControllerBase
{
    private readonly AdminServiceClient _adminClient;
    private readonly ILogger<InvitationsController> _logger;

    public InvitationsController(AdminServiceClient adminClient, ILogger<InvitationsController> logger)
    {
        _adminClient = adminClient;
        _logger = logger;
    }

    /// <summary>
    /// Get all pending invitations for current tenant or specified tenant
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<InvitationDto>>> GetInvitations([FromQuery] Guid? tenantId = null)
    {
        try
        {
            // If no tenant context and no tenantId filter, return empty for "All Tenants" mode
            // (invitations are tenant-specific and can't be shown across all tenants)
            var contextTenantId = HttpContext.GetTenantId();
            var isAllTenantsMode = HttpContext.IsAllTenantsMode();

            if (isAllTenantsMode && !tenantId.HasValue)
            {
                // In "All Tenants" mode without a filter, return empty list
                // because we can't fetch invitations for all tenants at once
                return Ok(new List<InvitationDto>());
            }

            var aclInvitations = await _adminClient.GetInvitationsAsync(tenantId);

            var invitations = aclInvitations.Select(inv => new InvitationDto
            {
                InvitationId = inv.InvitationId,
                Email = inv.Email,
                TenantId = inv.TenantId,
                TenantName = inv.TenantName,
                Status = inv.Status,
                ExpiresAt = inv.ExpiresAt,
                InvitedByName = inv.InvitedByName,
                CreatedAt = inv.CreatedAt
            }).ToList();

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
    public async Task<ActionResult<InvitationDto>> CreateInvitation([FromBody] CreateInvitationRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { error = "Email is required" });
            }

            var aclInvitation = await _adminClient.CreateInvitationAsync(request);

            var invitation = new InvitationDto
            {
                InvitationId = aclInvitation.InvitationId,
                Email = aclInvitation.Email,
                TenantId = aclInvitation.TenantId,
                TenantName = aclInvitation.TenantName,
                Status = aclInvitation.Status,
                ExpiresAt = aclInvitation.ExpiresAt,
                InvitedByName = aclInvitation.InvitedByName,
                CreatedAt = aclInvitation.CreatedAt
            };

            return CreatedAtAction(nameof(GetInvitations), invitation);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Conflict creating invitation");
            return Conflict(new { error = ex.Message });
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
            await _adminClient.RevokeInvitationAsync(id);
            return Ok(new { message = "Invitation revoked successfully", id });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Invitation not found");
            return NotFound(new { error = "Invitation not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking invitation");
            return BadRequest(new { error = ex.Message });
        }
    }
}
