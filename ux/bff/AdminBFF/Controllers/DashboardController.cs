using AdminBFF.Models;
using Microsoft.AspNetCore.Mvc;

namespace AdminBFF.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(ILogger<DashboardController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard aggregated data
    /// Combines stats from multiple domain services
    /// </summary>
    [HttpGet]
    public ActionResult<DashboardDto> GetDashboard()
    {
        try
        {
            // In production, these would be real API calls to domain services
            // For now, returning mock data
            var dashboard = new DashboardDto
            {
                Users = new UserStats
                {
                    Total = 1247,
                    Active = 892,
                    NewToday = 23
                },
                Orders = new OrderStats
                {
                    Total = 3421,
                    Pending = 45,
                    CompletedToday = 127
                },
                Products = new ProductStats
                {
                    Total = 856,
                    LowStock = 12,
                    OutOfStock = 3
                },
                Revenue = new RevenueStats
                {
                    Today = 45678.90m,
                    Week = 312456.50m,
                    Month = 1234567.89m
                }
            };

            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard data");
            return BadRequest(new { error = ex.Message });
        }
    }
}
