using AdminBFF.Models;
using Microsoft.AspNetCore.Mvc;

namespace AdminBFF.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PermissionsController : ControllerBase
{
    private readonly ILogger<PermissionsController> _logger;

    public PermissionsController(ILogger<PermissionsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get all permissions
    /// Proxied from Admin domain service
    /// </summary>
    [HttpGet]
    public ActionResult<List<PermissionDto>> GetPermissions()
    {
        try
        {
            // TODO: Call ADMIN_SERVICE_URL/permissions when implemented
            var permissions = new List<PermissionDto>
            {
                new PermissionDto { Id = "1", Resource = "users", Action = "read", Description = "View users" },
                new PermissionDto { Id = "2", Resource = "users", Action = "write", Description = "Create/Edit users" },
                new PermissionDto { Id = "3", Resource = "roles", Action = "read", Description = "View roles" },
                new PermissionDto { Id = "4", Resource = "roles", Action = "write", Description = "Create/Edit roles" },
                new PermissionDto { Id = "5", Resource = "products", Action = "read", Description = "View products" },
                new PermissionDto { Id = "6", Resource = "products", Action = "write", Description = "Create/Edit products" }
            };

            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching permissions");
            return BadRequest(new { error = ex.Message });
        }
    }
}
