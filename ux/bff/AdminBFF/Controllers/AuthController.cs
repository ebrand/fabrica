using AdminBFF.Services;
using AdminBFF.Models;
using Microsoft.AspNetCore.Mvc;

namespace AdminBFF.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AdminServiceClient _adminClient;
    private readonly ILogger<AuthController> _logger;
    private const string TenantCookieName = "X-Tenant-ID";
    private const string UserIdCookieName = "X-User-ID";
    private const string IsSystemAdminCookieName = "X-Is-System-Admin";

    public AuthController(AdminServiceClient adminClient, ILogger<AuthController> logger)
    {
        _adminClient = adminClient;
        _logger = logger;
    }

    /// <summary>
    /// Sync user from OAuth provider and return user info with tenants
    /// </summary>
    [HttpPost("sync")]
    public async Task<ActionResult<SyncUserResponseDto>> SyncUser([FromBody] SyncUserRequestDto request)
    {
        try
        {
            _logger.LogInformation("Syncing user: {Email}", request.Email);

            var response = await _adminClient.SyncUserAsync(request);

            if (response == null)
            {
                return BadRequest(new { error = "Failed to sync user" });
            }

            // Set user context cookies
            SetUserCookies(response.UserId.ToString(), response.IsSystemAdmin);

            // Check if there's an existing tenant cookie and validate it belongs to this user
            var existingTenantId = Request.Cookies[TenantCookieName];
            var tenantIds = response.Tenants.Select(t => t.TenantId.ToString()).ToHashSet();

            if (!string.IsNullOrEmpty(existingTenantId) && !tenantIds.Contains(existingTenantId))
            {
                // Existing tenant cookie doesn't belong to this user - clear it
                _logger.LogInformation("Clearing stale tenant cookie {TenantId} - user {Email} doesn't have access",
                    existingTenantId, request.Email);
                Response.Cookies.Delete(TenantCookieName);
            }

            // If user has exactly one tenant, auto-select it by setting cookie
            if (response.Tenants.Count == 1)
            {
                var tenant = response.Tenants[0];
                SetTenantCookie(tenant.TenantId.ToString());
                _logger.LogInformation("Auto-selected single tenant {TenantId} for user {Email}",
                    tenant.TenantId, request.Email);
            }

            _logger.LogInformation("User synced successfully: {UserId}, Tenants: {TenantCount}",
                response.UserId, response.Tenants.Count);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing user: {Email}", request.Email);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get user's tenants
    /// </summary>
    [HttpGet("tenants/{userId}")]
    public async Task<ActionResult<List<TenantAccessDto>>> GetUserTenants(Guid userId)
    {
        try
        {
            var tenants = await _adminClient.GetUserTenantsAsync(userId);
            return Ok(tenants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenants for user: {UserId}", userId);
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

    private void SetUserCookies(string userId, bool isSystemAdmin)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = false, // Set to true in production with HTTPS
            SameSite = SameSiteMode.Lax,
            Path = "/",
            MaxAge = TimeSpan.FromDays(30)
        };

        Response.Cookies.Append(UserIdCookieName, userId, cookieOptions);
        Response.Cookies.Append(IsSystemAdminCookieName, isSystemAdmin.ToString().ToLower(), cookieOptions);
    }
}
