namespace ContentBFF.Services;

public class ContentServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ContentServiceClient> _logger;
    private readonly string _contentServiceUrl;

    public ContentServiceClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ContentServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _contentServiceUrl = configuration["CONTENT_SERVICE_URL"] ?? "http://acl-content:3460";
        _httpClient.BaseAddress = new Uri(_contentServiceUrl);
    }

    public async Task<HttpResponseMessage> GetContentAsync(string? locale = null, int page = 1, int pageSize = 20)
    {
        var url = $"/api/content?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(locale))
        {
            url += $"&locale={locale}";
        }

        _logger.LogInformation("Fetching content from {Url}", url);
        return await _httpClient.GetAsync(url);
    }

    public async Task<HttpResponseMessage> GetContentBySlugAsync(string slug, string? locale = null)
    {
        var url = $"/api/content/slug/{slug}";
        if (!string.IsNullOrEmpty(locale))
        {
            url += $"?locale={locale}";
        }

        _logger.LogInformation("Fetching content by slug from {Url}", url);
        return await _httpClient.GetAsync(url);
    }

    public async Task<HttpResponseMessage> GetContentByIdAsync(Guid id, string? locale = null)
    {
        var url = $"/api/content/{id}";
        if (!string.IsNullOrEmpty(locale))
        {
            url += $"?locale={locale}";
        }

        _logger.LogInformation("Fetching content by id from {Url}", url);
        return await _httpClient.GetAsync(url);
    }

    public async Task<HttpResponseMessage> GetLanguagesAsync()
    {
        _logger.LogInformation("Fetching languages");
        return await _httpClient.GetAsync("/api/language");
    }

    public async Task<HttpResponseMessage> GetMenuAsync(string code, string? locale = null)
    {
        var url = $"/api/content/menu/{code}";
        if (!string.IsNullOrEmpty(locale))
        {
            url += $"?locale={locale}";
        }

        _logger.LogInformation("Fetching menu from {Url}", url);
        return await _httpClient.GetAsync(url);
    }

    public async Task<HttpResponseMessage> GetMediaAsync(Guid id)
    {
        _logger.LogInformation("Fetching media {Id}", id);
        return await _httpClient.GetAsync($"/api/content/media/{id}");
    }

    // Content Block methods
    public async Task<HttpResponseMessage> GetContentBlocksAsync(string? tenantId = null, string? localeCode = null, string? blockType = null, bool? isGlobal = null)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(tenantId)) queryParams.Add($"tenantId={tenantId}");
        if (!string.IsNullOrEmpty(localeCode)) queryParams.Add($"localeCode={localeCode}");
        if (!string.IsNullOrEmpty(blockType)) queryParams.Add($"blockType={blockType}");
        if (isGlobal.HasValue) queryParams.Add($"isGlobal={isGlobal.Value}");

        var url = "/api/contentblock";
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        _logger.LogInformation("Fetching content blocks from {Url}", url);
        return await _httpClient.GetAsync(url);
    }

    public async Task<HttpResponseMessage> GetContentBlockByIdAsync(Guid id, string? localeCode = null)
    {
        var url = $"/api/contentblock/{id}";
        if (!string.IsNullOrEmpty(localeCode))
        {
            url += $"?localeCode={localeCode}";
        }

        _logger.LogInformation("Fetching content block by id from {Url}", url);
        return await _httpClient.GetAsync(url);
    }

    public async Task<HttpResponseMessage> GetContentBlockByCodeAsync(string code, string? tenantId = null, string? localeCode = null)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(tenantId)) queryParams.Add($"tenantId={tenantId}");
        if (!string.IsNullOrEmpty(localeCode)) queryParams.Add($"localeCode={localeCode}");

        var url = $"/api/contentblock/code/{code}";
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        _logger.LogInformation("Fetching content block by code from {Url}", url);
        return await _httpClient.GetAsync(url);
    }
}
