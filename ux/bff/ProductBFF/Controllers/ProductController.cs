using Microsoft.AspNetCore.Mvc;
using ProductBFF.Services;
using System.Text;
using System.Text.Json;

namespace ProductBFF.Controllers;

[ApiController]
[Route("api/[controller]s")]
public class ProductController : ControllerBase
{
    private readonly ProductServiceClient _productService;
    private readonly ILogger<ProductController> _logger;

    public ProductController(ProductServiceClient productService, ILogger<ProductController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] Guid? tenantId = null, [FromQuery] string? status = null)
    {
        try
        {
            var response = await _productService.GetProductsAsync(tenantId, status);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var products = await response.Content.ReadFromJsonAsync<object>();
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProductById(Guid id)
    {
        try
        {
            var response = await _productService.GetProductByIdAsync(id);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var product = await response.Content.ReadFromJsonAsync<object>();
            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] JsonElement body)
    {
        try
        {
            var content = new StringContent(body.GetRawText(), Encoding.UTF8, "application/json");
            var response = await _productService.CreateProductAsync(content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var product = await response.Content.ReadFromJsonAsync<object>();
            return CreatedAtAction(nameof(GetProductById), new { id = product }, product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] JsonElement body)
    {
        try
        {
            var content = new StringContent(body.GetRawText(), Encoding.UTF8, "application/json");
            var response = await _productService.UpdateProductAsync(id, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        try
        {
            var response = await _productService.DeleteProductAsync(id);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
    }
}
