using System.Text;
using System.Text.Json;

namespace AdminBFF.Services;

/// <summary>
/// HTTP client for acl-content service
/// Used for cross-domain operations like provisioning languages during tenant onboarding
/// </summary>
public class ContentServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ContentServiceClient> _logger;
    private readonly ConfigurationService _configService;
    private bool _isInitialized = false;

    public ContentServiceClient(
        HttpClient httpClient,
        ILogger<ContentServiceClient> logger,
        ConfigurationService configService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configService = configService;
    }

    /// <summary>
    /// Initialize the HTTP client with the base URL from configuration
    /// </summary>
    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized) return;

        var contentServiceUrl = await _configService.GetServiceUrlAsync("content");
        _httpClient.BaseAddress = new Uri(contentServiceUrl);
        _isInitialized = true;
    }

    /// <summary>
    /// Create a language for a tenant
    /// </summary>
    public async Task<bool> CreateLanguageAsync(CreateLanguageRequest request)
    {
        await EnsureInitializedAsync();

        try
        {
            var json = JsonSerializer.Serialize(request);
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/language")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(httpRequest);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to create language {LocaleCode} for tenant {TenantId}: {Error}",
                    request.LocaleCode, request.TenantId, errorContent);
                return false;
            }

            _logger.LogInformation("Created language {LocaleCode} for tenant {TenantId}",
                request.LocaleCode, request.TenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating language {LocaleCode} for tenant {TenantId}",
                request.LocaleCode, request.TenantId);
            return false;
        }
    }

    /// <summary>
    /// Provision default languages for a new tenant
    /// Creates en-US, es-ES, and fr-FR
    /// </summary>
    public async Task<int> ProvisionDefaultLanguagesAsync(string tenantId)
    {
        _logger.LogInformation("Provisioning default languages for tenant {TenantId}", tenantId);

        var languages = new[]
        {
            new CreateLanguageRequest
            {
                TenantId = tenantId,
                LocaleCode = "en-US",
                LanguageCode = "en",
                Name = "English (US)",
                NativeName = "English",
                IsDefault = true,
                IsActive = true,
                Direction = "ltr",
                DateFormat = "MM/dd/yyyy",
                CurrencyCode = "USD",
                DisplayOrder = 1
            },
            new CreateLanguageRequest
            {
                TenantId = tenantId,
                LocaleCode = "es-ES",
                LanguageCode = "es",
                Name = "Spanish (Spain)",
                NativeName = "Español",
                IsDefault = false,
                IsActive = true,
                Direction = "ltr",
                DateFormat = "dd/MM/yyyy",
                CurrencyCode = "EUR",
                DisplayOrder = 2
            },
            new CreateLanguageRequest
            {
                TenantId = tenantId,
                LocaleCode = "fr-FR",
                LanguageCode = "fr",
                Name = "French (France)",
                NativeName = "Français",
                IsDefault = false,
                IsActive = true,
                Direction = "ltr",
                DateFormat = "dd/MM/yyyy",
                CurrencyCode = "EUR",
                DisplayOrder = 3
            }
        };

        var successCount = 0;
        foreach (var lang in languages)
        {
            if (await CreateLanguageAsync(lang))
            {
                successCount++;
            }
        }

        _logger.LogInformation("Provisioned {SuccessCount}/{TotalCount} languages for tenant {TenantId}",
            successCount, languages.Length, tenantId);

        return successCount;
    }
}

/// <summary>
/// Request to create a language
/// </summary>
public class CreateLanguageRequest
{
    public string TenantId { get; set; } = string.Empty;
    public string LocaleCode { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NativeName { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Direction { get; set; }
    public string? DateFormat { get; set; }
    public string? CurrencyCode { get; set; }
    public int DisplayOrder { get; set; }
}
