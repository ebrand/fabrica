using AdminBFF.Models;
using Microsoft.AspNetCore.Mvc;

namespace AdminBFF.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly ILogger<RolesController> _logger;

    public RolesController(ILogger<RolesController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get all roles
    /// Proxied from Admin domain service
    /// </summary>
    [HttpGet]
    public ActionResult<List<RoleDto>> GetRoles()
    {
        try
        {
            // TODO: Call ADMIN_SERVICE_URL/roles when implemented
            var roles = new List<RoleDto>
            {
                new RoleDto
                {
                    Id = "1",
                    Name = "Admin",
                    Description = "Full system access",
                    Permissions = new List<string> { "users:read", "users:write", "roles:read", "roles:write" }
                },
                new RoleDto
                {
                    Id = "2",
                    Name = "Manager",
                    Description = "Limited management access",
                    Permissions = new List<string> { "users:read", "products:read", "products:write" }
                },
                new RoleDto
                {
                    Id = "3",
                    Name = "Viewer",
                    Description = "Read-only access",
                    Permissions = new List<string> { "users:read", "products:read" }
                }
            };

            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching roles");
            return BadRequest(new { error = ex.Message });
        }
    }
}
