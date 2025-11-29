using Microsoft.AspNetCore.Mvc;
using CustomerBFF.Services;
using CustomerBFF.Middleware;
using System.Text;
using System.Text.Json;

namespace CustomerBFF.Controllers;

[ApiController]
[Route("api/[controller]s")]
public class CustomerController : ControllerBase
{
    private readonly CustomerServiceClient _customerService;
    private readonly ILogger<CustomerController> _logger;

    public CustomerController(CustomerServiceClient customerService, ILogger<CustomerController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    private string? GetTenantId() => HttpContext.Items["TenantId"]?.ToString();
    private bool IsAllTenantsMode() => HttpContext.IsAllTenantsMode();

    [HttpGet]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] string? tenantId = null)
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
            var response = await _customerService.GetCustomersAsync(status, search, tenantId);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var customers = await response.Content.ReadFromJsonAsync<object>();
            return Ok(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customers");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCustomerById(Guid id)
    {
        try
        {
            var response = await _customerService.GetCustomerByIdAsync(id);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var customer = await response.Content.ReadFromJsonAsync<object>();
            return Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customer {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateCustomer([FromBody] JsonElement body)
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
            var response = await _customerService.CreateCustomerAsync(content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var customer = await response.Content.ReadFromJsonAsync<JsonElement>();
            var customerId = customer.GetProperty("customerId").GetGuid();
            return CreatedAtAction(nameof(GetCustomerById), new { id = customerId }, customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCustomer(Guid id, [FromBody] JsonElement body)
    {
        try
        {
            var content = new StringContent(body.GetRawText(), Encoding.UTF8, "application/json");
            var response = await _customerService.UpdateCustomerAsync(id, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCustomer(Guid id)
    {
        try
        {
            var response = await _customerService.DeleteCustomerAsync(id);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer {Id}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    // Address endpoints
    [HttpGet("{customerId:guid}/addresses")]
    public async Task<IActionResult> GetCustomerAddresses(Guid customerId)
    {
        try
        {
            var response = await _customerService.GetCustomerAddressesAsync(customerId);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var addresses = await response.Content.ReadFromJsonAsync<object>();
            return Ok(addresses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching addresses for customer {CustomerId}", customerId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{customerId:guid}/addresses")]
    public async Task<IActionResult> CreateCustomerAddress(Guid customerId, [FromBody] JsonElement body)
    {
        try
        {
            var content = new StringContent(body.GetRawText(), Encoding.UTF8, "application/json");
            var response = await _customerService.CreateCustomerAddressAsync(customerId, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var address = await response.Content.ReadFromJsonAsync<object>();
            return Created($"/api/customers/{customerId}/addresses", address);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating address for customer {CustomerId}", customerId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{customerId:guid}/addresses/{addressId:guid}")]
    public async Task<IActionResult> UpdateCustomerAddress(Guid customerId, Guid addressId, [FromBody] JsonElement body)
    {
        try
        {
            var content = new StringContent(body.GetRawText(), Encoding.UTF8, "application/json");
            var response = await _customerService.UpdateCustomerAddressAsync(customerId, addressId, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating address {AddressId} for customer {CustomerId}", addressId, customerId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{customerId:guid}/addresses/{addressId:guid}")]
    public async Task<IActionResult> DeleteCustomerAddress(Guid customerId, Guid addressId)
    {
        try
        {
            var response = await _customerService.DeleteCustomerAddressAsync(customerId, addressId);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting address {AddressId} for customer {CustomerId}", addressId, customerId);
            return BadRequest(new { error = ex.Message });
        }
    }

    // Segment endpoints
    [HttpGet("segments")]
    public async Task<IActionResult> GetSegments([FromQuery] string? tenantId = null)
    {
        try
        {
            var contextTenantId = GetTenantId();
            var isAllTenants = IsAllTenantsMode();

            if (string.IsNullOrEmpty(contextTenantId) && !isAllTenants)
            {
                return BadRequest(new { error = "Tenant context required" });
            }

            var response = await _customerService.GetSegmentsAsync(tenantId);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error });
            }

            var segments = await response.Content.ReadFromJsonAsync<object>();
            return Ok(segments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching segments");
            return BadRequest(new { error = ex.Message });
        }
    }
}
