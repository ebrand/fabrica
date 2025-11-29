using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductDomainService.Data;
using ProductDomainService.Models;

namespace ProductDomainService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly ProductDbContext _context;
    private readonly ILogger<ProductController> _logger;

    public ProductController(ProductDbContext context, ILogger<ProductController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Checks if the caller is a System Admin via the X-Is-System-Admin header
    /// </summary>
    private bool IsCallerSystemAdmin()
    {
        return Request.Headers.TryGetValue("X-Is-System-Admin", out var value)
            && value.FirstOrDefault()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Gets the tenant ID from header
    /// Returns null if empty GUID (All Tenants mode for System Admins)
    /// </summary>
    private string? GetHeaderTenantId()
    {
        if (Request.Headers.TryGetValue("X-Tenant-ID", out var headerValue))
        {
            var headerTenantId = headerValue.FirstOrDefault();
            if (!string.IsNullOrEmpty(headerTenantId))
            {
                // Empty GUID means "All Tenants" mode - return null to skip filtering
                if (headerTenantId == "00000000-0000-0000-0000-000000000000")
                {
                    _logger.LogDebug("All Tenants mode detected from header");
                    return null;
                }
                return headerTenantId;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets the tenant ID, requiring it unless in System Admin "All Tenants" mode
    /// Query param tenantId takes precedence for filtering (from dropdown)
    /// </summary>
    private (string? tenantId, bool isAllTenantsMode) GetTenantContext(string? queryTenantId)
    {
        var headerTenantId = GetHeaderTenantId();
        var isAllTenantsMode = IsCallerSystemAdmin() && headerTenantId == null;

        // Query param takes precedence (explicit filter from dropdown)
        // Then fall back to header tenant
        var effectiveTenantId = queryTenantId ?? headerTenantId;

        // If we have a tenantId, use it for filtering (even in All Tenants mode)
        if (!string.IsNullOrEmpty(effectiveTenantId))
        {
            return (effectiveTenantId, isAllTenantsMode);
        }

        // If in All Tenants mode with no filter, return null to show all
        if (isAllTenantsMode)
        {
            return (null, true);
        }

        // Non-admin with no tenant - this is an error case
        return (null, false);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts([FromQuery] string? tenantId, [FromQuery] string? status)
    {
        try
        {
            var (effectiveTenantId, isAllTenantsMode) = GetTenantContext(tenantId);

            // Allow "All Tenants" mode for System Admins, otherwise require tenant
            if (string.IsNullOrEmpty(effectiveTenantId) && !isAllTenantsMode)
            {
                return BadRequest(new { error = "Tenant ID is required" });
            }

            IQueryable<Product> query = _context.Products;

            // Filter by tenant if a tenantId is provided
            // In "All Tenants" mode without a filter, return all products
            if (!string.IsNullOrEmpty(effectiveTenantId))
            {
                query = query.Where(p => p.TenantId == effectiveTenantId);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.Status == status);
            }

            var products = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            _logger.LogInformation("Fetched {Count} products, AllTenantsMode: {AllTenantsMode}, TenantId: {TenantId}",
                products.Count, isAllTenantsMode, effectiveTenantId ?? "all");
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(Guid id)
    {
        try
        {
            var tenantId = GetHeaderTenantId();

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound(new { error = "Product not found" });
            }

            // Verify tenant ownership if tenant context is provided
            if (!string.IsNullOrEmpty(tenantId) && product.TenantId != tenantId)
            {
                _logger.LogWarning("Tenant {TenantId} attempted to access product {ProductId} owned by {OwnerId}",
                    tenantId, id, product.TenantId);
                return NotFound(new { error = "Product not found" });
            }

            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product {ProductId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(Product product)
    {
        try
        {
            var tenantId = GetHeaderTenantId();

            // Require tenant context for creation
            if (string.IsNullOrEmpty(product.TenantId) && string.IsNullOrEmpty(tenantId))
            {
                return BadRequest(new { error = "Tenant ID is required" });
            }

            // Use header tenant if product doesn't specify one
            if (string.IsNullOrEmpty(product.TenantId))
            {
                product.TenantId = tenantId!;
            }

            product.Id = Guid.NewGuid();
            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created product {ProductId} for tenant {TenantId}", product.Id, product.TenantId);
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(Guid id, Product product)
    {
        try
        {
            if (id != product.Id)
            {
                return BadRequest(new { error = "Product ID mismatch" });
            }

            var tenantId = GetHeaderTenantId();
            var existingProduct = await _context.Products.FindAsync(id);

            if (existingProduct == null)
            {
                return NotFound(new { error = "Product not found" });
            }

            // Verify tenant ownership
            if (!string.IsNullOrEmpty(tenantId) && existingProduct.TenantId != tenantId)
            {
                _logger.LogWarning("Tenant {TenantId} attempted to update product {ProductId} owned by {OwnerId}",
                    tenantId, id, existingProduct.TenantId);
                return NotFound(new { error = "Product not found" });
            }

            // Preserve the original tenant ID
            product.TenantId = existingProduct.TenantId;
            product.UpdatedAt = DateTime.UtcNow;
            _context.Entry(existingProduct).CurrentValues.SetValues(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated product {ProductId} for tenant {TenantId}", id, existingProduct.TenantId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        try
        {
            var tenantId = GetHeaderTenantId();
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound(new { error = "Product not found" });
            }

            // Verify tenant ownership
            if (!string.IsNullOrEmpty(tenantId) && product.TenantId != tenantId)
            {
                _logger.LogWarning("Tenant {TenantId} attempted to delete product {ProductId} owned by {OwnerId}",
                    tenantId, id, product.TenantId);
                return NotFound(new { error = "Product not found" });
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted product {ProductId} from tenant {TenantId}", id, product.TenantId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {ProductId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }
}
