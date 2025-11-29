using Microsoft.AspNetCore.Mvc;
using ProductBFF.Services;
using ProductBFF.Middleware;
using System.Text;
using System.Text.Json;

namespace ProductBFF.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoryController : ControllerBase
{
    private readonly ProductServiceClient _productService;
    private readonly ILogger<CategoryController> _logger;

    public CategoryController(ProductServiceClient productService, ILogger<CategoryController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    private string? GetTenantId() => HttpContext.Items["TenantId"]?.ToString();
    private bool IsAllTenantsMode() => HttpContext.IsAllTenantsMode();

    [HttpGet]
    public async Task<IActionResult> GetCategories([FromQuery] Guid? parentId = null, [FromQuery] Guid? tenantId = null)
    {
        try
        {
            var contextTenantId = GetTenantId();
            var isAllTenants = IsAllTenantsMode();

            // Allow "All Tenants" mode for System Admins, otherwise require tenant
            if (string.IsNullOrEmpty(contextTenantId) && !isAllTenants)
            {
                return BadRequest(new { error = "Tenant context required" });
            }

            // If tenantId filter is explicitly provided (from dropdown), use that
            var response = await _productService.GetCategoriesAsync(parentId, tenantId);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var categories = await response.Content.ReadFromJsonAsync<object>();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching categories");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCategoryById(Guid id)
    {
        try
        {
            var response = await _productService.GetCategoryByIdAsync(id);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var category = await response.Content.ReadFromJsonAsync<object>();
            return Ok(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching category {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] JsonElement body)
    {
        try
        {
            var tenantId = GetTenantId();
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest(new { error = "Tenant context required" });
            }

            // Inject tenantId from context
            var bodyDict = JsonSerializer.Deserialize<Dictionary<string, object>>(body.GetRawText());
            if (bodyDict != null)
            {
                bodyDict["tenantId"] = tenantId;
            }
            var modifiedBody = JsonSerializer.Serialize(bodyDict);
            var content = new StringContent(modifiedBody, Encoding.UTF8, "application/json");
            var response = await _productService.CreateCategoryAsync(content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var category = await response.Content.ReadFromJsonAsync<object>();
            return CreatedAtAction(nameof(GetCategoryById), new { id = category }, category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] JsonElement body)
    {
        try
        {
            var tenantId = GetTenantId();
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest(new { error = "Tenant context required" });
            }

            // Inject id and tenantId from context
            var bodyDict = JsonSerializer.Deserialize<Dictionary<string, object>>(body.GetRawText());
            if (bodyDict != null)
            {
                bodyDict["id"] = id.ToString();
                bodyDict["tenantId"] = tenantId;
            }
            var modifiedBody = JsonSerializer.Serialize(bodyDict);
            var content = new StringContent(modifiedBody, Encoding.UTF8, "application/json");
            var response = await _productService.UpdateCategoryAsync(id, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        try
        {
            var tenantId = GetTenantId();
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest(new { error = "Tenant context required" });
            }

            var response = await _productService.DeleteCategoryAsync(id);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
    }
}
