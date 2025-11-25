namespace ProductBFF.Services;

public class ProductServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductServiceClient> _logger;
    private readonly string _productServiceUrl;

    public ProductServiceClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ProductServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _productServiceUrl = configuration["PRODUCT_SERVICE_URL"] ?? "http://acl-product:3420";
        _httpClient.BaseAddress = new Uri(_productServiceUrl);
    }

    // Product endpoints
    public async Task<HttpResponseMessage> GetProductsAsync(Guid? tenantId = null, string? status = null)
    {
        var query = new List<string>();
        if (tenantId.HasValue)
            query.Add($"tenantId={tenantId}");
        if (!string.IsNullOrEmpty(status))
            query.Add($"status={status}");

        var url = "/api/product" + (query.Any() ? "?" + string.Join("&", query) : "");
        _logger.LogInformation("Fetching products from {Url}", url);
        return await _httpClient.GetAsync(url);
    }

    public async Task<HttpResponseMessage> GetProductByIdAsync(Guid id)
    {
        _logger.LogInformation("Fetching product {Id}", id);
        return await _httpClient.GetAsync($"/api/product/{id}");
    }

    public async Task<HttpResponseMessage> CreateProductAsync(HttpContent content)
    {
        _logger.LogInformation("Creating product");
        return await _httpClient.PostAsync("/api/product", content);
    }

    public async Task<HttpResponseMessage> UpdateProductAsync(Guid id, HttpContent content)
    {
        _logger.LogInformation("Updating product {Id}", id);
        return await _httpClient.PutAsync($"/api/product/{id}", content);
    }

    public async Task<HttpResponseMessage> DeleteProductAsync(Guid id)
    {
        _logger.LogInformation("Deleting product {Id}", id);
        return await _httpClient.DeleteAsync($"/api/product/{id}");
    }

    // Category endpoints
    public async Task<HttpResponseMessage> GetCategoriesAsync(Guid? tenantId = null, Guid? parentId = null)
    {
        var query = new List<string>();
        if (tenantId.HasValue)
            query.Add($"tenantId={tenantId}");
        if (parentId.HasValue)
            query.Add($"parentId={parentId}");

        var url = "/api/category" + (query.Any() ? "?" + string.Join("&", query) : "");
        _logger.LogInformation("Fetching categories from {Url}", url);
        return await _httpClient.GetAsync(url);
    }

    public async Task<HttpResponseMessage> GetCategoryByIdAsync(Guid id)
    {
        _logger.LogInformation("Fetching category {Id}", id);
        return await _httpClient.GetAsync($"/api/category/{id}");
    }

    public async Task<HttpResponseMessage> CreateCategoryAsync(HttpContent content)
    {
        _logger.LogInformation("Creating category");
        return await _httpClient.PostAsync("/api/category", content);
    }

    public async Task<HttpResponseMessage> UpdateCategoryAsync(Guid id, HttpContent content)
    {
        _logger.LogInformation("Updating category {Id}", id);
        return await _httpClient.PutAsync($"/api/category/{id}", content);
    }

    public async Task<HttpResponseMessage> DeleteCategoryAsync(Guid id)
    {
        _logger.LogInformation("Deleting category {Id}", id);
        return await _httpClient.DeleteAsync($"/api/category/{id}");
    }
}
