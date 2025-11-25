using Microsoft.AspNetCore.Mvc;
using ProductBFF.Services;
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

    [HttpGet]
    public async Task<IActionResult> GetCategories([FromQuery] Guid? tenantId = null, [FromQuery] Guid? parentId = null)
    {
        try
        {
            var response = await _productService.GetCategoriesAsync(tenantId, parentId);

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
            var content = new StringContent(body.GetRawText(), Encoding.UTF8, "application/json");
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
            var content = new StringContent(body.GetRawText(), Encoding.UTF8, "application/json");
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
