using CustomerBFF.Middleware;

namespace CustomerBFF.Services;

public class CustomerServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CustomerServiceClient> _logger;
    private readonly string _customerServiceUrl;
    private const string AllTenantsGuid = "00000000-0000-0000-0000-000000000000";

    public CustomerServiceClient(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILogger<CustomerServiceClient> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _customerServiceUrl = configuration["CUSTOMER_SERVICE_URL"] ?? "http://acl-customer:3410";
        _httpClient.BaseAddress = new Uri(_customerServiceUrl);
    }

    private string? GetTenantId()
    {
        return _httpContextAccessor.HttpContext?.Items["TenantId"]?.ToString();
    }

    /// <summary>
    /// Checks if "All Tenants" mode is active (System Admin cross-tenant access)
    /// </summary>
    private bool IsAllTenantsMode()
    {
        return _httpContextAccessor.HttpContext?.Items["IsAllTenantsMode"] as bool? ?? false;
    }

    /// <summary>
    /// Gets tenant ID for filtering - returns null if in "All Tenants" mode
    /// </summary>
    private string? GetTenantIdForFilter()
    {
        if (IsAllTenantsMode())
        {
            return null; // No tenant filtering for System Admins in "All Tenants" mode
        }
        return GetTenantId();
    }

    private void AddTenantHeader(HttpRequestMessage request)
    {
        var tenantId = GetTenantId();
        // Only add tenant header if not in "All Tenants" mode
        if (!string.IsNullOrEmpty(tenantId) && tenantId != AllTenantsGuid)
        {
            request.Headers.Add("X-Tenant-ID", tenantId);
        }
        // For "All Tenants" mode, add headers to indicate System Admin access
        if (IsAllTenantsMode())
        {
            request.Headers.Add("X-All-Tenants", "true");
            request.Headers.Add("X-Is-System-Admin", "true");
        }
    }

    // Customer endpoints
    public async Task<HttpResponseMessage> GetCustomersAsync(string? status = null, string? search = null, string? filterTenantId = null)
    {
        var isAllTenants = IsAllTenantsMode();
        var query = new List<string>();

        // If explicit filterTenantId is provided (from dropdown), use that
        // Otherwise, use automatic tenant detection (unless in All Tenants mode)
        if (!string.IsNullOrEmpty(filterTenantId))
        {
            query.Add($"tenantId={filterTenantId}");
        }
        else
        {
            var tenantIdForFilter = GetTenantIdForFilter();
            if (!string.IsNullOrEmpty(tenantIdForFilter))
                query.Add($"tenantId={tenantIdForFilter}");
        }

        if (!string.IsNullOrEmpty(status))
            query.Add($"status={status}");

        if (!string.IsNullOrEmpty(search))
            query.Add($"search={Uri.EscapeDataString(search)}");

        var url = "/api/customer" + (query.Any() ? "?" + string.Join("&", query) : "");
        _logger.LogInformation("Fetching customers from {Url}, AllTenantsMode: {AllTenantsMode}, FilterTenantId: {FilterTenantId}", url, isAllTenants, filterTenantId);

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddTenantHeader(request);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> GetCustomerByIdAsync(Guid id)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Fetching customer {Id} for tenant {TenantId}", id, tenantId);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/customer/{id}?tenantId={tenantId}");
        AddTenantHeader(request);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> CreateCustomerAsync(HttpContent content)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Creating customer for tenant {TenantId}", tenantId);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/customer")
        {
            Content = content
        };
        AddTenantHeader(request);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> UpdateCustomerAsync(Guid id, HttpContent content)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Updating customer {Id} for tenant {TenantId}", id, tenantId);

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/customer/{id}")
        {
            Content = content
        };
        AddTenantHeader(request);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> DeleteCustomerAsync(Guid id)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Deleting customer {Id} for tenant {TenantId}", id, tenantId);

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/customer/{id}");
        AddTenantHeader(request);
        return await _httpClient.SendAsync(request);
    }

    // Address endpoints
    public async Task<HttpResponseMessage> GetCustomerAddressesAsync(Guid customerId)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Fetching addresses for customer {CustomerId}, tenant {TenantId}", customerId, tenantId);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/customer/{customerId}/addresses");
        AddTenantHeader(request);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> CreateCustomerAddressAsync(Guid customerId, HttpContent content)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Creating address for customer {CustomerId}, tenant {TenantId}", customerId, tenantId);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/customer/{customerId}/addresses")
        {
            Content = content
        };
        AddTenantHeader(request);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> UpdateCustomerAddressAsync(Guid customerId, Guid addressId, HttpContent content)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Updating address {AddressId} for customer {CustomerId}, tenant {TenantId}", addressId, customerId, tenantId);

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/customer/{customerId}/addresses/{addressId}")
        {
            Content = content
        };
        AddTenantHeader(request);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> DeleteCustomerAddressAsync(Guid customerId, Guid addressId)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Deleting address {AddressId} for customer {CustomerId}, tenant {TenantId}", addressId, customerId, tenantId);

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/customer/{customerId}/addresses/{addressId}");
        AddTenantHeader(request);
        return await _httpClient.SendAsync(request);
    }

    // Segment endpoints
    public async Task<HttpResponseMessage> GetSegmentsAsync(string? filterTenantId = null)
    {
        var isAllTenants = IsAllTenantsMode();
        var query = new List<string>();

        if (!string.IsNullOrEmpty(filterTenantId))
        {
            query.Add($"tenantId={filterTenantId}");
        }
        else
        {
            var tenantIdForFilter = GetTenantIdForFilter();
            if (!string.IsNullOrEmpty(tenantIdForFilter))
                query.Add($"tenantId={tenantIdForFilter}");
        }

        var url = "/api/customer/segments" + (query.Any() ? "?" + string.Join("&", query) : "");
        _logger.LogInformation("Fetching segments from {Url}, AllTenantsMode: {AllTenantsMode}", url, isAllTenants);

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddTenantHeader(request);
        return await _httpClient.SendAsync(request);
    }
}
