using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductDomainService.Data;
using ProductDomainService.Models;

namespace ProductDomainService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoryController : ControllerBase
{
    private readonly ProductDbContext _context;
    private readonly ILogger<CategoryController> _logger;

    public CategoryController(ProductDbContext context, ILogger<CategoryController> logger)
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
    public async Task<ActionResult<IEnumerable<Category>>> GetCategories([FromQuery] string? tenantId, [FromQuery] Guid? parentId)
    {
        try
        {
            var (effectiveTenantId, isAllTenantsMode) = GetTenantContext(tenantId);

            // Allow "All Tenants" mode for System Admins, otherwise require tenant
            if (string.IsNullOrEmpty(effectiveTenantId) && !isAllTenantsMode)
            {
                return BadRequest(new { error = "Tenant ID is required" });
            }

            IQueryable<Category> query = _context.Categories;

            // Filter by tenant if a tenantId is provided
            // In "All Tenants" mode without a filter, return all categories
            if (!string.IsNullOrEmpty(effectiveTenantId))
            {
                query = query.Where(c => c.TenantId == effectiveTenantId);
            }

            if (parentId.HasValue)
            {
                query = query.Where(c => c.ParentId == parentId.Value);
            }
            else
            {
                // Return top-level categories if no parent specified
                query = query.Where(c => c.ParentId == null);
            }

            var categories = await query
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            _logger.LogInformation("Fetched {Count} categories, AllTenantsMode: {AllTenantsMode}, TenantId: {TenantId}",
                categories.Count, isAllTenantsMode, effectiveTenantId ?? "all");
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Category>> GetCategory(Guid id)
    {
        try
        {
            var tenantId = GetHeaderTenantId();

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound(new { error = "Category not found" });
            }

            // Verify tenant ownership if tenant context is provided
            if (!string.IsNullOrEmpty(tenantId) && category.TenantId != tenantId)
            {
                _logger.LogWarning("Tenant {TenantId} attempted to access category {CategoryId} owned by {OwnerId}",
                    tenantId, id, category.TenantId);
                return NotFound(new { error = "Category not found" });
            }

            return Ok(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category {CategoryId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<Category>> CreateCategory(Category category)
    {
        try
        {
            var tenantId = GetHeaderTenantId();

            // Require tenant context for creation
            if (string.IsNullOrEmpty(category.TenantId) && string.IsNullOrEmpty(tenantId))
            {
                return BadRequest(new { error = "Tenant ID is required" });
            }

            // Use header tenant if category doesn't specify one
            if (string.IsNullOrEmpty(category.TenantId))
            {
                category.TenantId = tenantId!;
            }

            category.Id = Guid.NewGuid();
            category.CreatedAt = DateTime.UtcNow;
            category.UpdatedAt = DateTime.UtcNow;

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created category {CategoryId} for tenant {TenantId}", category.Id, category.TenantId);
            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(Guid id, Category category)
    {
        try
        {
            if (id != category.Id)
            {
                return BadRequest(new { error = "Category ID mismatch" });
            }

            var tenantId = GetHeaderTenantId();
            var existingCategory = await _context.Categories.FindAsync(id);

            if (existingCategory == null)
            {
                return NotFound(new { error = "Category not found" });
            }

            // Verify tenant ownership
            if (!string.IsNullOrEmpty(tenantId) && existingCategory.TenantId != tenantId)
            {
                _logger.LogWarning("Tenant {TenantId} attempted to update category {CategoryId} owned by {OwnerId}",
                    tenantId, id, existingCategory.TenantId);
                return NotFound(new { error = "Category not found" });
            }

            // Preserve the original tenant ID
            category.TenantId = existingCategory.TenantId;
            category.UpdatedAt = DateTime.UtcNow;
            _context.Entry(existingCategory).CurrentValues.SetValues(category);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated category {CategoryId} for tenant {TenantId}", id, existingCategory.TenantId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {CategoryId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        try
        {
            var tenantId = GetHeaderTenantId();

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound(new { error = "Category not found" });
            }

            // Verify tenant ownership
            if (!string.IsNullOrEmpty(tenantId) && category.TenantId != tenantId)
            {
                _logger.LogWarning("Tenant {TenantId} attempted to delete category {CategoryId} owned by {OwnerId}",
                    tenantId, id, category.TenantId);
                return NotFound(new { error = "Category not found" });
            }

            // Check if category has child categories
            var hasChildren = await _context.Categories.AnyAsync(c => c.ParentId == id);
            if (hasChildren)
            {
                return BadRequest(new { error = "Cannot delete category with child categories" });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted category {CategoryId} from tenant {TenantId}", id, category.TenantId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {CategoryId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }
}
