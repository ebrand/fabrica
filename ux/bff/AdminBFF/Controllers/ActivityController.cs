using AdminBFF.Models;
using Microsoft.AspNetCore.Mvc;

namespace AdminBFF.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActivityController : ControllerBase
{
    private readonly ILogger<ActivityController> _logger;

    public ActivityController(ILogger<ActivityController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get activity log
    /// Aggregated from multiple services
    /// </summary>
    [HttpGet]
    public ActionResult<List<ActivityDto>> GetActivity()
    {
        try
        {
            var activities = new List<ActivityDto>
            {
                new ActivityDto
                {
                    Id = "1",
                    User = "John Doe",
                    Action = "Updated user permissions",
                    Resource = "User: Jane Smith",
                    Timestamp = DateTime.Parse("2025-11-22T14:30:00Z")
                },
                new ActivityDto
                {
                    Id = "2",
                    User = "Jane Smith",
                    Action = "Created new product",
                    Resource = "Product: Widget Pro",
                    Timestamp = DateTime.Parse("2025-11-22T13:15:00Z")
                },
                new ActivityDto
                {
                    Id = "3",
                    User = "Admin",
                    Action = "Modified role permissions",
                    Resource = "Role: Manager",
                    Timestamp = DateTime.Parse("2025-11-22T12:00:00Z")
                }
            };

            return Ok(activities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching activity");
            return BadRequest(new { error = ex.Message });
        }
    }
}
