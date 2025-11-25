using AdminBFF.Models;
using System.Text.Json;

namespace AdminBFF.Services;

/// <summary>
/// HTTP client for acl-admin service
/// </summary>
public class AdminServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AdminServiceClient> _logger;
    private readonly ConfigurationService _configService;
    private bool _isInitialized = false;

    public AdminServiceClient(
        HttpClient httpClient,
        ILogger<AdminServiceClient> logger,
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

        var adminServiceUrl = await _configService.GetServiceUrlAsync("admin");
        _httpClient.BaseAddress = new Uri(adminServiceUrl);
        _isInitialized = true;
    }

    /// <summary>
    /// Get all users from acl-admin service
    /// </summary>
    public async Task<List<AclUserResponse>> GetUsersAsync()
    {
        await EnsureInitializedAsync();

        try
        {
            var response = await _httpClient.GetAsync("/api/user");
            response.EnsureSuccessStatusCode();

            var users = await response.Content.ReadFromJsonAsync<List<AclUserResponse>>();
            return users ?? new List<AclUserResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching users from acl-admin");
            throw;
        }
    }

    /// <summary>
    /// Get single user from acl-admin service
    /// </summary>
    public async Task<AclUserResponse?> GetUserAsync(string id)
    {
        await EnsureInitializedAsync();

        try
        {
            var response = await _httpClient.GetAsync($"/api/user/{id}");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<AclUserResponse>();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user from acl-admin");
            throw;
        }
    }

    /// <summary>
    /// Create user in acl-admin service
    /// </summary>
    public async Task<AclUserResponse> CreateUserAsync(AclCreateUserPayload payload)
    {
        await EnsureInitializedAsync();

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/user", payload);
            response.EnsureSuccessStatusCode();

            var user = await response.Content.ReadFromJsonAsync<AclUserResponse>();
            return user ?? throw new Exception("Failed to deserialize user response");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            throw new InvalidOperationException("Email already exists");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user in acl-admin");
            throw;
        }
    }

    /// <summary>
    /// Update user in acl-admin service
    /// </summary>
    public async Task<AclUserResponse> UpdateUserAsync(string id, AclUpdateUserPayload payload)
    {
        await EnsureInitializedAsync();

        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/user/{id}", payload);
            response.EnsureSuccessStatusCode();

            var user = await response.Content.ReadFromJsonAsync<AclUserResponse>();
            return user ?? throw new Exception("Failed to deserialize user response");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException("User not found");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            throw new InvalidOperationException("Email already exists");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user in acl-admin");
            throw;
        }
    }

    /// <summary>
    /// Delete user in acl-admin service
    /// </summary>
    public async Task DeleteUserAsync(string id)
    {
        await EnsureInitializedAsync();

        try
        {
            var response = await _httpClient.DeleteAsync($"/api/user/{id}");
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException("User not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user in acl-admin");
            throw;
        }
    }
}
