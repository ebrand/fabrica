using System.Net.Http.Json;
using ContentBFF.Middleware;

namespace ContentBFF.Services;

public class ContentServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ContentServiceClient> _logger;
    private readonly string _contentServiceUrl;

    public ContentServiceClient(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILogger<ContentServiceClient> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _contentServiceUrl = configuration["CONTENT_SERVICE_URL"] ?? "http://acl-content:3460";
        _httpClient.BaseAddress = new Uri(_contentServiceUrl);
    }

    /// <summary>
    /// Add authorization headers to outgoing requests
    /// </summary>
    private void AddAuthHeaders(HttpRequestMessage request)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var isSystemAdmin = httpContext.IsCurrentUserSystemAdmin();
            request.Headers.Add("X-Is-System-Admin", isSystemAdmin.ToString().ToLower());

            var userId = httpContext.GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                request.Headers.Add("X-User-ID", userId);
            }

            // Pass tenant context - null/empty means "All Tenants" mode for System Admins
            var tenantId = httpContext.GetTenantId();
            if (!string.IsNullOrEmpty(tenantId))
            {
                request.Headers.Add("X-Tenant-ID", tenantId);
            }
        }
    }

    /// <summary>
    /// Helper to create a request with auth headers
    /// </summary>
    private HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        AddAuthHeaders(request);
        return request;
    }

    /// <summary>
    /// Helper to create a POST request with JSON body and auth headers
    /// </summary>
    private HttpRequestMessage CreateJsonRequest(HttpMethod method, string url, object? body = null)
    {
        var request = CreateRequest(method, url);
        if (body != null)
        {
            request.Content = JsonContent.Create(body);
        }
        return request;
    }

    public async Task<HttpResponseMessage> GetContentAsync(string? locale = null, int page = 1, int pageSize = 20)
    {
        var url = $"/api/content?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(locale))
        {
            url += $"&locale={locale}";
        }

        _logger.LogInformation("Fetching content from {Url}", url);
        var request = CreateRequest(HttpMethod.Get, url);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> GetContentBySlugAsync(string slug, string? locale = null)
    {
        var url = $"/api/content/slug/{slug}";
        if (!string.IsNullOrEmpty(locale))
        {
            url += $"?locale={locale}";
        }

        _logger.LogInformation("Fetching content by slug from {Url}", url);
        var request = CreateRequest(HttpMethod.Get, url);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> GetContentByIdAsync(Guid id, string? locale = null)
    {
        var url = $"/api/content/{id}";
        if (!string.IsNullOrEmpty(locale))
        {
            url += $"?locale={locale}";
        }

        _logger.LogInformation("Fetching content by id from {Url}", url);
        var request = CreateRequest(HttpMethod.Get, url);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> GetLanguagesAsync(string? tenantId = null)
    {
        var url = "/api/language";
        if (!string.IsNullOrEmpty(tenantId))
        {
            url += $"?tenantId={tenantId}";
        }

        _logger.LogInformation("Fetching languages from {Url}", url);
        var request = CreateRequest(HttpMethod.Get, url);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> GetLanguageByIdAsync(Guid id)
    {
        _logger.LogInformation("Fetching language {LanguageId}", id);
        var request = CreateRequest(HttpMethod.Get, $"/api/language/{id}");
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> CreateLanguageAsync(object language)
    {
        _logger.LogInformation("Creating new language");
        var request = CreateJsonRequest(HttpMethod.Post, "/api/language", language);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> UpdateLanguageAsync(Guid id, object language)
    {
        _logger.LogInformation("Updating language {LanguageId}", id);
        var request = CreateJsonRequest(HttpMethod.Put, $"/api/language/{id}", language);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> DeleteLanguageAsync(Guid id)
    {
        _logger.LogInformation("Deleting language {LanguageId}", id);
        var request = CreateRequest(HttpMethod.Delete, $"/api/language/{id}");
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> GetMenuAsync(string code, string? locale = null)
    {
        var url = $"/api/content/menu/{code}";
        if (!string.IsNullOrEmpty(locale))
        {
            url += $"?locale={locale}";
        }

        _logger.LogInformation("Fetching menu from {Url}", url);
        var request = CreateRequest(HttpMethod.Get, url);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> GetMediaAsync(Guid id)
    {
        _logger.LogInformation("Fetching media {Id}", id);
        var request = CreateRequest(HttpMethod.Get, $"/api/media/{id}");
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> UploadMediaAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string? tenantId = null,
        Guid? uploadedBy = null)
    {
        _logger.LogInformation("Uploading media: {FileName}", fileName);

        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", fileName);

        if (!string.IsNullOrEmpty(tenantId))
        {
            content.Add(new StringContent(tenantId), "tenantId");
        }
        if (uploadedBy.HasValue)
        {
            content.Add(new StringContent(uploadedBy.Value.ToString()), "uploadedBy");
        }

        return await _httpClient.PostAsync("/api/media/upload", content);
    }

    public async Task<HttpResponseMessage> DeleteMediaAsync(Guid id)
    {
        _logger.LogInformation("Deleting media {Id}", id);
        var request = CreateRequest(HttpMethod.Delete, $"/api/media/{id}");
        return await _httpClient.SendAsync(request);
    }

    // Block Content methods
    public async Task<HttpResponseMessage> GetBlockContentsAsync(string? tenantId = null, string? localeCode = null, string? blockSlug = null)
    {
        var queryParams = new List<string>();
        // Note: tenantId from query param is deprecated - now using X-Tenant-ID header
        if (!string.IsNullOrEmpty(tenantId)) queryParams.Add($"tenantId={tenantId}");
        if (!string.IsNullOrEmpty(localeCode)) queryParams.Add($"localeCode={localeCode}");
        if (!string.IsNullOrEmpty(blockSlug)) queryParams.Add($"blockSlug={blockSlug}");

        var url = "/api/contentblock";
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        _logger.LogInformation("Fetching block contents from {Url}", url);
        var request = CreateRequest(HttpMethod.Get, url);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> GetBlockContentByIdAsync(Guid id, string? localeCode = null)
    {
        var url = $"/api/contentblock/{id}";
        if (!string.IsNullOrEmpty(localeCode))
        {
            url += $"?localeCode={localeCode}";
        }

        _logger.LogInformation("Fetching block content by id from {Url}", url);
        var request = CreateRequest(HttpMethod.Get, url);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> GetBlockContentByCodeAsync(string code, string? tenantId = null, string? localeCode = null, string? variant = null)
    {
        var queryParams = new List<string>();
        // Note: tenantId from query param is deprecated - now using X-Tenant-ID header
        if (!string.IsNullOrEmpty(tenantId)) queryParams.Add($"tenantId={tenantId}");
        if (!string.IsNullOrEmpty(localeCode)) queryParams.Add($"localeCode={localeCode}");
        if (!string.IsNullOrEmpty(variant)) queryParams.Add($"variant={variant}");

        var url = $"/api/contentblock/code/{code}";
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        _logger.LogInformation("Fetching block content by code from {Url}", url);
        var request = CreateRequest(HttpMethod.Get, url);
        return await _httpClient.SendAsync(request);
    }

    // Block templates and schema methods
    public async Task<HttpResponseMessage> GetBlocksAsync(string? tenantId = null)
    {
        var url = "/api/contentblock/blocks";
        // Note: tenantId from query param is deprecated - now using X-Tenant-ID header
        if (!string.IsNullOrEmpty(tenantId))
        {
            url += $"?tenantId={tenantId}";
        }

        _logger.LogInformation("Fetching block templates from {Url}", url);
        var request = CreateRequest(HttpMethod.Get, url);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> GetSectionTypesAsync(string? tenantId = null)
    {
        var url = "/api/contentblock/section-types";
        // Note: tenantId from query param is deprecated - now using X-Tenant-ID header
        if (!string.IsNullOrEmpty(tenantId))
        {
            url += $"?tenantId={tenantId}";
        }

        _logger.LogInformation("Fetching section types from {Url}", url);
        var request = CreateRequest(HttpMethod.Get, url);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> GetBlockVariantsAsync(string blockSlug, string? tenantId = null)
    {
        var url = $"/api/contentblock/blocks/{blockSlug}/variants";
        if (!string.IsNullOrEmpty(tenantId))
        {
            url += $"?tenantId={tenantId}";
        }

        _logger.LogInformation("Fetching variants for block {BlockSlug} from {Url}", blockSlug, url);
        var request = CreateRequest(HttpMethod.Get, url);
        return await _httpClient.SendAsync(request);
    }

    // Block CRUD methods
    public async Task<HttpResponseMessage> CreateBlockAsync(object block)
    {
        _logger.LogInformation("Creating new block");
        var request = CreateJsonRequest(HttpMethod.Post, "/api/contentblock/blocks", block);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> UpdateBlockAsync(Guid id, object block)
    {
        _logger.LogInformation("Updating block {BlockId}", id);
        var request = CreateJsonRequest(HttpMethod.Put, $"/api/contentblock/blocks/{id}", block);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> DeleteBlockAsync(Guid id)
    {
        _logger.LogInformation("Deleting block {BlockId}", id);
        var request = CreateRequest(HttpMethod.Delete, $"/api/contentblock/blocks/{id}");
        return await _httpClient.SendAsync(request);
    }

    // Block-Section management methods
    public async Task<HttpResponseMessage> AddSectionToBlockAsync(Guid blockId, Guid sectionTypeId, object? body = null)
    {
        _logger.LogInformation("Adding section {SectionTypeId} to block {BlockId}", sectionTypeId, blockId);
        var request = CreateJsonRequest(HttpMethod.Post, $"/api/contentblock/blocks/{blockId}/sections/{sectionTypeId}", body ?? new { });
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> RemoveSectionFromBlockAsync(Guid blockId, Guid sectionTypeId)
    {
        _logger.LogInformation("Removing section {SectionTypeId} from block {BlockId}", sectionTypeId, blockId);
        var request = CreateRequest(HttpMethod.Delete, $"/api/contentblock/blocks/{blockId}/sections/{sectionTypeId}");
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> UpdateBlockSectionAsync(Guid blockId, Guid sectionTypeId, object body)
    {
        _logger.LogInformation("Updating section {SectionTypeId} on block {BlockId}", sectionTypeId, blockId);
        var request = CreateJsonRequest(HttpMethod.Put, $"/api/contentblock/blocks/{blockId}/sections/{sectionTypeId}", body);
        return await _httpClient.SendAsync(request);
    }

    // Block Content CRUD methods
    public async Task<HttpResponseMessage> CreateBlockContentAsync(object content)
    {
        _logger.LogInformation("Creating new block content");
        var request = CreateJsonRequest(HttpMethod.Post, "/api/contentblock", content);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> UpdateBlockContentAsync(Guid id, object content)
    {
        _logger.LogInformation("Updating block content {ContentId}", id);
        var request = CreateJsonRequest(HttpMethod.Put, $"/api/contentblock/{id}", content);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> DeleteBlockContentAsync(Guid id)
    {
        _logger.LogInformation("Deleting block content {ContentId}", id);
        var request = CreateRequest(HttpMethod.Delete, $"/api/contentblock/{id}");
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> GetBlockContentForEditAsync(Guid id)
    {
        _logger.LogInformation("Fetching block content for edit {ContentId}", id);
        var request = CreateRequest(HttpMethod.Get, $"/api/contentblock/{id}/edit");
        return await _httpClient.SendAsync(request);
    }

    // Variant CRUD methods
    public async Task<HttpResponseMessage> CreateVariantAsync(Guid blockId, object variant)
    {
        _logger.LogInformation("Creating new variant for block {BlockId}", blockId);
        var request = CreateJsonRequest(HttpMethod.Post, $"/api/contentblock/blocks/{blockId}/variants", variant);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> DeleteVariantAsync(Guid blockId, Guid variantId)
    {
        _logger.LogInformation("Deleting variant {VariantId} from block {BlockId}", variantId, blockId);
        var request = CreateRequest(HttpMethod.Delete, $"/api/contentblock/blocks/{blockId}/variants/{variantId}");
        return await _httpClient.SendAsync(request);
    }

    // Section Type CRUD methods
    public async Task<HttpResponseMessage> CreateSectionTypeAsync(object sectionType)
    {
        _logger.LogInformation("Creating new section type");
        var request = CreateJsonRequest(HttpMethod.Post, "/api/contentblock/section-types", sectionType);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> UpdateSectionTypeAsync(Guid id, object sectionType)
    {
        _logger.LogInformation("Updating section type {SectionTypeId}", id);
        var request = CreateJsonRequest(HttpMethod.Put, $"/api/contentblock/section-types/{id}", sectionType);
        return await _httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> DeleteSectionTypeAsync(Guid id)
    {
        _logger.LogInformation("Deleting section type {SectionTypeId}", id);
        var request = CreateRequest(HttpMethod.Delete, $"/api/contentblock/section-types/{id}");
        return await _httpClient.SendAsync(request);
    }
}
