using AdminBFF.Services;
using AdminBFF.Models;
using AdminBFF.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace AdminBFF.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly AdminServiceClient _adminClient;
    private readonly ILogger<TenantsController> _logger;
    private const string TenantCookieName = "X-Tenant-ID";

    public TenantsController(AdminServiceClient adminClient, ILogger<TenantsController> logger)
    {
        _adminClient = adminClient;
        _logger = logger;
    }

    /// <summary>
    /// Select a tenant (sets the tenant cookie)
    /// Guid.Empty (00000000-0000-0000-0000-000000000000) is valid for "All Tenants" mode (System Admins)
    /// </summary>
    [HttpPost("select")]
    public ActionResult SelectTenant([FromBody] SelectTenantRequest request)
    {
        try
        {
            // Note: Guid.Empty is valid - it represents "All Tenants" mode for System Admins
            SetTenantCookie(request.TenantId.ToString());

            var isAllTenantsMode = request.TenantId == Guid.Empty;
            _logger.LogInformation("Tenant selected: {TenantId}, AllTenantsMode: {AllTenantsMode}",
                request.TenantId, isAllTenantsMode);

            return Ok(new { message = "Tenant selected", tenantId = request.TenantId, isAllTenantsMode });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting tenant: {TenantId}", request.TenantId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get the currently selected tenant ID from cookie
    /// </summary>
    [HttpGet("current")]
    public ActionResult GetCurrentTenant()
    {
        var tenantId = HttpContext.GetTenantId();

        if (string.IsNullOrEmpty(tenantId))
        {
            return Ok(new { tenantId = (string?)null, selected = false });
        }

        return Ok(new { tenantId, selected = true });
    }

    /// <summary>
    /// Clear tenant selection (removes cookie)
    /// </summary>
    [HttpPost("clear")]
    public ActionResult ClearTenant()
    {
        Response.Cookies.Delete(TenantCookieName);
        _logger.LogInformation("Tenant selection cleared");
        return Ok(new { message = "Tenant selection cleared" });
    }

    /// <summary>
    /// Get all tenants (admin only)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<TenantDto>>> GetTenants([FromQuery] bool includeInactive = false)
    {
        try
        {
            var tenants = await _adminClient.GetTenantsAsync(includeInactive);
            return Ok(tenants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tenants");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get tenant by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TenantDto>> GetTenant(Guid id)
    {
        try
        {
            var tenant = await _adminClient.GetTenantAsync(id);
            if (tenant == null)
            {
                return NotFound(new { error = "Tenant not found" });
            }
            return Ok(tenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tenant: {TenantId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Create a new tenant
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TenantDto>> CreateTenant([FromBody] CreateTenantRequest request)
    {
        try
        {
            var tenant = await _adminClient.CreateTenantAsync(request);
            return CreatedAtAction(nameof(GetTenant), new { id = tenant.TenantId }, tenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update a tenant
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateTenant(Guid id, [FromBody] UpdateTenantRequest request)
    {
        try
        {
            await _adminClient.UpdateTenantAsync(id, request);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant: {TenantId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a tenant
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTenant(Guid id)
    {
        try
        {
            await _adminClient.DeleteTenantAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tenant: {TenantId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    private void SetTenantCookie(string tenantId)
    {
        Response.Cookies.Append(TenantCookieName, tenantId, new CookieOptions
        {
            HttpOnly = true,
            Secure = false, // Set to true in production with HTTPS
            SameSite = SameSiteMode.Lax,
            Path = "/",
            MaxAge = TimeSpan.FromDays(30)
        });
    }
}

public class SelectTenantRequest
{
    public Guid TenantId { get; set; }
}
