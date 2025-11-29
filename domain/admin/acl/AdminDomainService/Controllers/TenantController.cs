using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdminDomainService.Data;
using AdminDomainService.Models;

namespace AdminDomainService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantController : ControllerBase
{
    private readonly AdminDbContext _context;
    private readonly ILogger<TenantController> _logger;

    public TenantController(AdminDbContext context, ILogger<TenantController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets all tenants (admin only)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Tenant>>> GetTenants([FromQuery] bool includeInactive = false)
    {
        try
        {
            var query = _context.Tenants.AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(t => t.IsActive);
            }

            var tenants = await query
                .OrderBy(t => t.Name)
                .ToListAsync();

            return Ok(tenants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenants");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a tenant by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Tenant>> GetTenant(Guid id)
    {
        try
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.TenantId == id);

            if (tenant == null)
            {
                return NotFound(new { error = "Tenant not found" });
            }

            return Ok(tenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant {TenantId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a tenant by slug
    /// </summary>
    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<Tenant>> GetTenantBySlug(string slug)
    {
        try
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Slug == slug);

            if (tenant == null)
            {
                return NotFound(new { error = "Tenant not found" });
            }

            return Ok(tenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant by slug {Slug}", slug);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new tenant
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Tenant>> CreateTenant(CreateTenantDto dto)
    {
        try
        {
            // Check for duplicate slug
            if (await _context.Tenants.AnyAsync(t => t.Slug == dto.Slug))
            {
                return BadRequest(new { error = "A tenant with this slug already exists" });
            }

            var tenant = new Tenant
            {
                TenantId = Guid.NewGuid(),
                Name = dto.Name,
                Slug = dto.Slug,
                Description = dto.Description,
                LogoMediaId = dto.LogoMediaId,
                IsActive = true,
                IsPersonal = false,
                OwnerUserId = dto.OwnerUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = dto.CreatedBy
            };

            _context.Tenants.Add(tenant);

            // If owner is specified, add them to the tenant as owner
            if (dto.OwnerUserId.HasValue)
            {
                var userTenant = new UserTenant
                {
                    UserTenantId = Guid.NewGuid(),
                    UserId = dto.OwnerUserId.Value,
                    TenantId = tenant.TenantId,
                    Role = "owner",
                    IsActive = true,
                    GrantedBy = dto.CreatedBy,
                    GrantedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.UserTenants.Add(userTenant);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Created tenant: {TenantId}, Name: {Name}, Slug: {Slug}",
                tenant.TenantId, tenant.Name, tenant.Slug);

            return CreatedAtAction(nameof(GetTenant), new { id = tenant.TenantId }, tenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing tenant
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTenant(Guid id, UpdateTenantDto dto)
    {
        try
        {
            var tenant = await _context.Tenants.FindAsync(id);

            if (tenant == null)
            {
                return NotFound(new { error = "Tenant not found" });
            }

            // Check for duplicate slug if slug is being changed
            if (!string.IsNullOrEmpty(dto.Slug) && dto.Slug != tenant.Slug)
            {
                if (await _context.Tenants.AnyAsync(t => t.Slug == dto.Slug && t.TenantId != id))
                {
                    return BadRequest(new { error = "A tenant with this slug already exists" });
                }
                tenant.Slug = dto.Slug;
            }

            if (!string.IsNullOrEmpty(dto.Name))
            {
                tenant.Name = dto.Name;
            }

            if (dto.Description != null)
            {
                tenant.Description = dto.Description;
            }

            if (dto.LogoMediaId.HasValue)
            {
                tenant.LogoMediaId = dto.LogoMediaId;
            }

            if (dto.IsActive.HasValue)
            {
                tenant.IsActive = dto.IsActive.Value;
            }

            if (dto.Settings != null)
            {
                tenant.Settings = dto.Settings;
            }

            tenant.UpdatedAt = DateTime.UtcNow;
            tenant.UpdatedBy = dto.UpdatedBy;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated tenant: {TenantId}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant {TenantId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a tenant (soft delete by setting IsActive = false)
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTenant(Guid id)
    {
        try
        {
            var tenant = await _context.Tenants.FindAsync(id);

            if (tenant == null)
            {
                return NotFound(new { error = "Tenant not found" });
            }

            // Don't allow deleting the system tenant
            if (tenant.Slug == "system")
            {
                return BadRequest(new { error = "Cannot delete the system tenant" });
            }

            // Soft delete
            tenant.IsActive = false;
            tenant.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted tenant: {TenantId}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tenant {TenantId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets all users in a tenant
    /// </summary>
    [HttpGet("{id:guid}/users")]
    public async Task<ActionResult<IEnumerable<object>>> GetTenantUsers(Guid id)
    {
        try
        {
            var tenant = await _context.Tenants.FindAsync(id);

            if (tenant == null)
            {
                return NotFound(new { error = "Tenant not found" });
            }

            var users = await _context.UserTenants
                .Where(ut => ut.TenantId == id && ut.IsActive)
                .Include(ut => ut.User)
                .Where(ut => ut.User != null && ut.User.IsActive)
                .Select(ut => new
                {
                    ut.User!.UserId,
                    ut.User.Email,
                    ut.User.FirstName,
                    ut.User.LastName,
                    ut.User.DisplayName,
                    ut.User.AvatarMediaId,
                    ut.Role,
                    ut.GrantedAt
                })
                .ToListAsync();

            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users for tenant {TenantId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Adds a user to a tenant
    /// </summary>
    [HttpPost("{id:guid}/users")]
    public async Task<IActionResult> AddUserToTenant(Guid id, AddUserToTenantDto dto)
    {
        try
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null)
            {
                return NotFound(new { error = "Tenant not found" });
            }

            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            // Check if user is already in tenant
            var existing = await _context.UserTenants
                .FirstOrDefaultAsync(ut => ut.TenantId == id && ut.UserId == dto.UserId);

            if (existing != null)
            {
                if (existing.IsActive)
                {
                    return BadRequest(new { error = "User is already a member of this tenant" });
                }
                // Reactivate if previously removed
                existing.IsActive = true;
                existing.Role = dto.Role ?? "member";
                existing.RevokedAt = null;
                existing.RevokedBy = null;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var userTenant = new UserTenant
                {
                    UserTenantId = Guid.NewGuid(),
                    UserId = dto.UserId,
                    TenantId = id,
                    Role = dto.Role ?? "member",
                    IsActive = true,
                    GrantedBy = dto.GrantedBy,
                    GrantedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.UserTenants.Add(userTenant);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Added user {UserId} to tenant {TenantId} with role {Role}",
                dto.UserId, id, dto.Role);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user {UserId} to tenant {TenantId}", dto.UserId, id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Removes a user from a tenant
    /// </summary>
    [HttpDelete("{id:guid}/users/{userId:guid}")]
    public async Task<IActionResult> RemoveUserFromTenant(Guid id, Guid userId, [FromQuery] Guid? removedBy = null)
    {
        try
        {
            var userTenant = await _context.UserTenants
                .FirstOrDefaultAsync(ut => ut.TenantId == id && ut.UserId == userId && ut.IsActive);

            if (userTenant == null)
            {
                return NotFound(new { error = "User is not a member of this tenant" });
            }

            // Soft delete
            userTenant.IsActive = false;
            userTenant.RevokedAt = DateTime.UtcNow;
            userTenant.RevokedBy = removedBy;
            userTenant.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Removed user {UserId} from tenant {TenantId}", userId, id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user {UserId} from tenant {TenantId}", userId, id);
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class CreateTenantDto
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? LogoMediaId { get; set; }
    public Guid? OwnerUserId { get; set; }
    public Guid? CreatedBy { get; set; }
}

public class UpdateTenantDto
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public Guid? LogoMediaId { get; set; }
    public bool? IsActive { get; set; }
    public string? Settings { get; set; }
    public Guid? UpdatedBy { get; set; }
}

public class AddUserToTenantDto
{
    public Guid UserId { get; set; }
    public string? Role { get; set; }
    public Guid? GrantedBy { get; set; }
}
