using ProductBFF.Middleware;

namespace ProductBFF.Services;

public class ProductServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ProductServiceClient> _logger;
    private readonly string _productServiceUrl;
    private const string AllTenantsGuid = "00000000-0000-0000-0000-000000000000";

    public ProductServiceClient(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILogger<ProductServiceClient> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _productServiceUrl = configuration["PRODUCT_SERVICE_URL"] ?? "http://acl-product:3420";
        _httpClient.BaseAddress = new Uri(_productServiceUrl);
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
        // For "All Tenants" mode, add a special header to indicate admin access
        if (IsAllTenantsMode())
        {
            request.Headers.Add("X-All-Tenants", "true");
        }
    }

    // Product endpoints
    public async Task<HttpResponseMessage> GetProductsAsync(string? status = null, Guid? filterTenantId = null)
    {
        var isAllTenants = IsAllTenantsMode();
        var query = new List<string>();

        // If explicit filterTenantId is provided (from dropdown), use that
        // Otherwise, use automatic tenant detection (unless in All Tenants mode)
        if (filterTenantId.HasValue)
        {
            query.Add($"tenantId={filterTenantId.Value}");
        }
        else
        {
            var tenantIdForFilter = GetTenantIdForFilter();
            if (!string.IsNullOrEmpty(tenantIdForFilter))
                query.Add($"tenantId={tenantIdForFilter}");
        }

        if (!string.IsNullOrEmpty(status))
            query.Add($"status={status}");

        var url = "/api/product" + (query.Any() ? "?" + string.Join("&", query) : "");
        _logger.LogInformation("Fetching products from {Url}, AllTenantsMode: {AllTenantsMode}, FilterTenantId: {FilterTenantId}", url, isAllTenants, filterTenantId);

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddTenantHeader(request);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> GetProductByIdAsync(Guid id)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Fetching product {Id} for tenant {TenantId}", id, tenantId);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/product/{id}?tenantId={tenantId}");
        AddTenantHeader(request);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> CreateProductAsync(HttpContent content)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Creating product for tenant {TenantId}", tenantId);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/product")
        {
            Content = content
        };
        AddTenantHeader(request);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> UpdateProductAsync(Guid id, HttpContent content)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Updating product {Id} for tenant {TenantId}", id, tenantId);

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/product/{id}")
        {
            Content = content
        };
        AddTenantHeader(request);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> DeleteProductAsync(Guid id)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Deleting product {Id} for tenant {TenantId}", id, tenantId);

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/product/{id}");
        AddTenantHeader(request);
        return await _httpClient.SendAsync(request);
    }

    // Category endpoints
    public async Task<HttpResponseMessage> GetCategoriesAsync(Guid? parentId = null, Guid? filterTenantId = null)
    {
        var isAllTenants = IsAllTenantsMode();
        var query = new List<string>();

        // If explicit filterTenantId is provided (from dropdown), use that
        // Otherwise, use automatic tenant detection (unless in All Tenants mode)
        if (filterTenantId.HasValue)
        {
            query.Add($"tenantId={filterTenantId.Value}");
        }
        else
        {
            var tenantIdForFilter = GetTenantIdForFilter();
            if (!string.IsNullOrEmpty(tenantIdForFilter))
                query.Add($"tenantId={tenantIdForFilter}");
        }

        if (parentId.HasValue)
            query.Add($"parentId={parentId}");

        var url = "/api/category" + (query.Any() ? "?" + string.Join("&", query) : "");
        _logger.LogInformation("Fetching categories from {Url}, AllTenantsMode: {AllTenantsMode}, FilterTenantId: {FilterTenantId}", url, isAllTenants, filterTenantId);

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddTenantHeader(request);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> GetCategoryByIdAsync(Guid id)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Fetching category {Id} for tenant {TenantId}", id, tenantId);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/category/{id}?tenantId={tenantId}");
        AddTenantHeader(request);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> CreateCategoryAsync(HttpContent content)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Creating category for tenant {TenantId}", tenantId);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/category")
        {
            Content = content
        };
        AddTenantHeader(request);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> UpdateCategoryAsync(Guid id, HttpContent content)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Updating category {Id} for tenant {TenantId}", id, tenantId);

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/category/{id}")
        {
            Content = content
        };
        AddTenantHeader(request);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> DeleteCategoryAsync(Guid id)
    {
        var tenantId = GetTenantId();
        _logger.LogInformation("Deleting category {Id} for tenant {TenantId}", id, tenantId);

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/category/{id}");
        AddTenantHeader(request);
        return await _httpClient.SendAsync(request);
    }
}
