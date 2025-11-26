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

    // Block Content methods
    public async Task<HttpResponseMessage> GetBlockContentsAsync(string? tenantId = null, string? localeCode = null, string? blockSlug = null)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(tenantId)) queryParams.Add($"tenantId={tenantId}");
        if (!string.IsNullOrEmpty(localeCode)) queryParams.Add($"localeCode={localeCode}");
        if (!string.IsNullOrEmpty(blockSlug)) queryParams.Add($"blockSlug={blockSlug}");

        var url = "/api/contentblock";
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        _logger.LogInformation("Fetching block contents from {Url}", url);
        return await _httpClient.GetAsync(url);
    }

    public async Task<HttpResponseMessage> GetBlockContentByIdAsync(Guid id, string? localeCode = null)
    {
        var url = $"/api/contentblock/{id}";
        if (!string.IsNullOrEmpty(localeCode))
        {
            url += $"?localeCode={localeCode}";
        }

        _logger.LogInformation("Fetching block content by id from {Url}", url);
        return await _httpClient.GetAsync(url);
    }

    public async Task<HttpResponseMessage> GetBlockContentByCodeAsync(string code, string? tenantId = null, string? localeCode = null, string? variant = null)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(tenantId)) queryParams.Add($"tenantId={tenantId}");
        if (!string.IsNullOrEmpty(localeCode)) queryParams.Add($"localeCode={localeCode}");
        if (!string.IsNullOrEmpty(variant)) queryParams.Add($"variant={variant}");

        var url = $"/api/contentblock/code/{code}";
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        _logger.LogInformation("Fetching block content by code from {Url}", url);
        return await _httpClient.GetAsync(url);
    }

    // Block templates and schema methods
    public async Task<HttpResponseMessage> GetBlocksAsync(string? tenantId = null)
    {
        var url = "/api/contentblock/blocks";
        if (!string.IsNullOrEmpty(tenantId))
        {
            url += $"?tenantId={tenantId}";
        }

        _logger.LogInformation("Fetching block templates from {Url}", url);
        return await _httpClient.GetAsync(url);
    }

    public async Task<HttpResponseMessage> GetSectionTypesAsync(string? tenantId = null)
    {
        var url = "/api/contentblock/section-types";
        if (!string.IsNullOrEmpty(tenantId))
        {
            url += $"?tenantId={tenantId}";
        }

        _logger.LogInformation("Fetching section types from {Url}", url);
        return await _httpClient.GetAsync(url);
    }

    public async Task<HttpResponseMessage> GetBlockVariantsAsync(string blockSlug, string? tenantId = null)
    {
        var url = $"/api/contentblock/blocks/{blockSlug}/variants";
        if (!string.IsNullOrEmpty(tenantId))
        {
            url += $"?tenantId={tenantId}";
        }

        _logger.LogInformation("Fetching variants for block {BlockSlug} from {Url}", blockSlug, url);
        return await _httpClient.GetAsync(url);
    }

    // Block CRUD methods
    public async Task<HttpResponseMessage> CreateBlockAsync(object block)
    {
        _logger.LogInformation("Creating new block");
        return await _httpClient.PostAsJsonAsync("/api/contentblock/blocks", block);
    }

    public async Task<HttpResponseMessage> UpdateBlockAsync(Guid id, object block)
    {
        _logger.LogInformation("Updating block {BlockId}", id);
        return await _httpClient.PutAsJsonAsync($"/api/contentblock/blocks/{id}", block);
    }

    public async Task<HttpResponseMessage> DeleteBlockAsync(Guid id)
    {
        _logger.LogInformation("Deleting block {BlockId}", id);
        return await _httpClient.DeleteAsync($"/api/contentblock/blocks/{id}");
    }

    // Block-Section management methods
    public async Task<HttpResponseMessage> AddSectionToBlockAsync(Guid blockId, Guid sectionTypeId, object? request = null)
    {
        _logger.LogInformation("Adding section {SectionTypeId} to block {BlockId}", sectionTypeId, blockId);
        return await _httpClient.PostAsJsonAsync($"/api/contentblock/blocks/{blockId}/sections/{sectionTypeId}", request ?? new { });
    }

    public async Task<HttpResponseMessage> RemoveSectionFromBlockAsync(Guid blockId, Guid sectionTypeId)
    {
        _logger.LogInformation("Removing section {SectionTypeId} from block {BlockId}", sectionTypeId, blockId);
        return await _httpClient.DeleteAsync($"/api/contentblock/blocks/{blockId}/sections/{sectionTypeId}");
    }

    public async Task<HttpResponseMessage> UpdateBlockSectionAsync(Guid blockId, Guid sectionTypeId, object request)
    {
        _logger.LogInformation("Updating section {SectionTypeId} on block {BlockId}", sectionTypeId, blockId);
        return await _httpClient.PutAsJsonAsync($"/api/contentblock/blocks/{blockId}/sections/{sectionTypeId}", request);
    }

    // Block Content CRUD methods
    public async Task<HttpResponseMessage> CreateBlockContentAsync(object content)
    {
        _logger.LogInformation("Creating new block content");
        return await _httpClient.PostAsJsonAsync("/api/contentblock", content);
    }

    public async Task<HttpResponseMessage> UpdateBlockContentAsync(Guid id, object content)
    {
        _logger.LogInformation("Updating block content {ContentId}", id);
        return await _httpClient.PutAsJsonAsync($"/api/contentblock/{id}", content);
    }

    public async Task<HttpResponseMessage> DeleteBlockContentAsync(Guid id)
    {
        _logger.LogInformation("Deleting block content {ContentId}", id);
        return await _httpClient.DeleteAsync($"/api/contentblock/{id}");
    }

    public async Task<HttpResponseMessage> GetBlockContentForEditAsync(Guid id)
    {
        _logger.LogInformation("Fetching block content for edit {ContentId}", id);
        return await _httpClient.GetAsync($"/api/contentblock/{id}/edit");
    }
}
