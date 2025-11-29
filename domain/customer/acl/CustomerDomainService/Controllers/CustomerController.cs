using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CustomerDomainService.Data;
using CustomerDomainService.Models;

namespace CustomerDomainService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerController : ControllerBase
{
    private readonly CustomerDbContext _context;
    private readonly ILogger<CustomerController> _logger;

    public CustomerController(CustomerDbContext context, ILogger<CustomerController> logger)
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
    public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers(
        [FromQuery] string? tenantId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var (effectiveTenantId, isAllTenantsMode) = GetTenantContext(tenantId);

            // Allow "All Tenants" mode for System Admins, otherwise require tenant
            if (string.IsNullOrEmpty(effectiveTenantId) && !isAllTenantsMode)
            {
                return BadRequest(new { error = "Tenant ID is required" });
            }

            IQueryable<Customer> query = _context.Customers
                .Include(c => c.Addresses);

            // Filter by tenant if a tenantId is provided
            if (!string.IsNullOrEmpty(effectiveTenantId))
            {
                query = query.Where(c => c.TenantId == effectiveTenantId);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(c => c.Status == status);
            }

            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(c =>
                    c.Email.ToLower().Contains(searchLower) ||
                    c.FirstName.ToLower().Contains(searchLower) ||
                    c.LastName.ToLower().Contains(searchLower) ||
                    (c.DisplayName != null && c.DisplayName.ToLower().Contains(searchLower)));
            }

            var totalCount = await query.CountAsync();
            var customers = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            _logger.LogInformation("Fetched {Count} customers, AllTenantsMode: {AllTenantsMode}, TenantId: {TenantId}",
                customers.Count, isAllTenantsMode, effectiveTenantId ?? "all");

            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            return Ok(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customers");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Customer>> GetCustomer(Guid id)
    {
        try
        {
            var tenantId = GetHeaderTenantId();

            var customer = await _context.Customers
                .Include(c => c.Addresses)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
            {
                return NotFound(new { error = "Customer not found" });
            }

            // Verify tenant ownership if tenant context is provided
            if (!string.IsNullOrEmpty(tenantId) && customer.TenantId != tenantId)
            {
                _logger.LogWarning("Tenant {TenantId} attempted to access customer {CustomerId} owned by {OwnerId}",
                    tenantId, id, customer.TenantId);
                return NotFound(new { error = "Customer not found" });
            }

            return Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer {CustomerId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<Customer>> CreateCustomer(Customer customer)
    {
        try
        {
            var tenantId = GetHeaderTenantId();

            // Require tenant context for creation
            if (string.IsNullOrEmpty(customer.TenantId) && string.IsNullOrEmpty(tenantId))
            {
                return BadRequest(new { error = "Tenant ID is required" });
            }

            // Use header tenant if customer doesn't specify one
            if (string.IsNullOrEmpty(customer.TenantId))
            {
                customer.TenantId = tenantId!;
            }

            // Check for duplicate email
            var existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.TenantId == customer.TenantId && c.Email == customer.Email);

            if (existingCustomer != null)
            {
                return BadRequest(new { error = "A customer with this email already exists" });
            }

            customer.Id = Guid.NewGuid();
            customer.CreatedAt = DateTime.UtcNow;
            customer.UpdatedAt = DateTime.UtcNow;

            // Set display name if not provided
            if (string.IsNullOrEmpty(customer.DisplayName))
            {
                customer.DisplayName = $"{customer.FirstName} {customer.LastName}".Trim();
            }

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created customer {CustomerId} for tenant {TenantId}", customer.Id, customer.TenantId);
            return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(Guid id, Customer customer)
    {
        try
        {
            if (id != customer.Id)
            {
                return BadRequest(new { error = "Customer ID mismatch" });
            }

            var tenantId = GetHeaderTenantId();
            var existingCustomer = await _context.Customers.FindAsync(id);

            if (existingCustomer == null)
            {
                return NotFound(new { error = "Customer not found" });
            }

            // Verify tenant ownership
            if (!string.IsNullOrEmpty(tenantId) && existingCustomer.TenantId != tenantId)
            {
                _logger.LogWarning("Tenant {TenantId} attempted to update customer {CustomerId} owned by {OwnerId}",
                    tenantId, id, existingCustomer.TenantId);
                return NotFound(new { error = "Customer not found" });
            }

            // Check for duplicate email if changed
            if (customer.Email != existingCustomer.Email)
            {
                var duplicateEmail = await _context.Customers
                    .FirstOrDefaultAsync(c => c.TenantId == existingCustomer.TenantId && c.Email == customer.Email && c.Id != id);

                if (duplicateEmail != null)
                {
                    return BadRequest(new { error = "A customer with this email already exists" });
                }
            }

            // Preserve the original tenant ID
            customer.TenantId = existingCustomer.TenantId;
            customer.UpdatedAt = DateTime.UtcNow;
            _context.Entry(existingCustomer).CurrentValues.SetValues(customer);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated customer {CustomerId} for tenant {TenantId}", id, existingCustomer.TenantId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {CustomerId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCustomer(Guid id)
    {
        try
        {
            var tenantId = GetHeaderTenantId();
            var customer = await _context.Customers.FindAsync(id);

            if (customer == null)
            {
                return NotFound(new { error = "Customer not found" });
            }

            // Verify tenant ownership
            if (!string.IsNullOrEmpty(tenantId) && customer.TenantId != tenantId)
            {
                _logger.LogWarning("Tenant {TenantId} attempted to delete customer {CustomerId} owned by {OwnerId}",
                    tenantId, id, customer.TenantId);
                return NotFound(new { error = "Customer not found" });
            }

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted customer {CustomerId} from tenant {TenantId}", id, customer.TenantId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer {CustomerId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    // ==========================================
    // ADDRESS ENDPOINTS
    // ==========================================

    [HttpGet("{customerId}/addresses")]
    public async Task<ActionResult<IEnumerable<CustomerAddress>>> GetCustomerAddresses(Guid customerId)
    {
        try
        {
            var tenantId = GetHeaderTenantId();
            var customer = await _context.Customers.FindAsync(customerId);

            if (customer == null)
            {
                return NotFound(new { error = "Customer not found" });
            }

            // Verify tenant ownership
            if (!string.IsNullOrEmpty(tenantId) && customer.TenantId != tenantId)
            {
                return NotFound(new { error = "Customer not found" });
            }

            var addresses = await _context.CustomerAddresses
                .Where(a => a.CustomerId == customerId)
                .OrderByDescending(a => a.IsDefault)
                .ThenBy(a => a.AddressType)
                .ToListAsync();

            return Ok(addresses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting addresses for customer {CustomerId}", customerId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{customerId}/addresses")]
    public async Task<ActionResult<CustomerAddress>> CreateCustomerAddress(Guid customerId, CustomerAddress address)
    {
        try
        {
            var tenantId = GetHeaderTenantId();
            var customer = await _context.Customers.FindAsync(customerId);

            if (customer == null)
            {
                return NotFound(new { error = "Customer not found" });
            }

            // Verify tenant ownership
            if (!string.IsNullOrEmpty(tenantId) && customer.TenantId != tenantId)
            {
                return NotFound(new { error = "Customer not found" });
            }

            address.Id = Guid.NewGuid();
            address.CustomerId = customerId;
            address.CreatedAt = DateTime.UtcNow;
            address.UpdatedAt = DateTime.UtcNow;

            // If this is marked as default, unset other defaults of the same type
            if (address.IsDefault)
            {
                var otherAddresses = await _context.CustomerAddresses
                    .Where(a => a.CustomerId == customerId && a.AddressType == address.AddressType && a.IsDefault)
                    .ToListAsync();

                foreach (var other in otherAddresses)
                {
                    other.IsDefault = false;
                }
            }

            _context.CustomerAddresses.Add(address);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created address {AddressId} for customer {CustomerId}", address.Id, customerId);
            return CreatedAtAction(nameof(GetCustomerAddresses), new { customerId }, address);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating address for customer {CustomerId}", customerId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{customerId}/addresses/{addressId}")]
    public async Task<IActionResult> UpdateCustomerAddress(Guid customerId, Guid addressId, CustomerAddress address)
    {
        try
        {
            if (addressId != address.Id)
            {
                return BadRequest(new { error = "Address ID mismatch" });
            }

            var tenantId = GetHeaderTenantId();
            var customer = await _context.Customers.FindAsync(customerId);

            if (customer == null)
            {
                return NotFound(new { error = "Customer not found" });
            }

            // Verify tenant ownership
            if (!string.IsNullOrEmpty(tenantId) && customer.TenantId != tenantId)
            {
                return NotFound(new { error = "Customer not found" });
            }

            var existingAddress = await _context.CustomerAddresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.CustomerId == customerId);

            if (existingAddress == null)
            {
                return NotFound(new { error = "Address not found" });
            }

            // If this is being set as default, unset other defaults of the same type
            if (address.IsDefault && !existingAddress.IsDefault)
            {
                var otherAddresses = await _context.CustomerAddresses
                    .Where(a => a.CustomerId == customerId && a.AddressType == address.AddressType && a.IsDefault)
                    .ToListAsync();

                foreach (var other in otherAddresses)
                {
                    other.IsDefault = false;
                }
            }

            address.CustomerId = customerId;
            address.UpdatedAt = DateTime.UtcNow;
            _context.Entry(existingAddress).CurrentValues.SetValues(address);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated address {AddressId} for customer {CustomerId}", addressId, customerId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating address {AddressId} for customer {CustomerId}", addressId, customerId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{customerId}/addresses/{addressId}")]
    public async Task<IActionResult> DeleteCustomerAddress(Guid customerId, Guid addressId)
    {
        try
        {
            var tenantId = GetHeaderTenantId();
            var customer = await _context.Customers.FindAsync(customerId);

            if (customer == null)
            {
                return NotFound(new { error = "Customer not found" });
            }

            // Verify tenant ownership
            if (!string.IsNullOrEmpty(tenantId) && customer.TenantId != tenantId)
            {
                return NotFound(new { error = "Customer not found" });
            }

            var address = await _context.CustomerAddresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.CustomerId == customerId);

            if (address == null)
            {
                return NotFound(new { error = "Address not found" });
            }

            _context.CustomerAddresses.Remove(address);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted address {AddressId} from customer {CustomerId}", addressId, customerId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting address {AddressId} from customer {CustomerId}", addressId, customerId);
            return BadRequest(new { error = ex.Message });
        }
    }

    // ==========================================
    // SEGMENT ENDPOINTS
    // ==========================================

    [HttpGet("segments")]
    public async Task<ActionResult<IEnumerable<CustomerSegment>>> GetSegments([FromQuery] string? tenantId = null)
    {
        try
        {
            var (effectiveTenantId, isAllTenantsMode) = GetTenantContext(tenantId);

            if (string.IsNullOrEmpty(effectiveTenantId) && !isAllTenantsMode)
            {
                return BadRequest(new { error = "Tenant ID is required" });
            }

            IQueryable<CustomerSegment> query = _context.CustomerSegments;

            if (!string.IsNullOrEmpty(effectiveTenantId))
            {
                query = query.Where(s => s.TenantId == effectiveTenantId);
            }

            var segments = await query.OrderBy(s => s.Name).ToListAsync();
            return Ok(segments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer segments");
            return BadRequest(new { error = ex.Message });
        }
    }
}
