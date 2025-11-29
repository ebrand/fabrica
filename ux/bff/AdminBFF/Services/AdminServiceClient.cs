using AdminBFF.Models;
using AdminBFF.Middleware;
using System.Text;
using System.Text.Json;

namespace AdminBFF.Services;

/// <summary>
/// HTTP client for acl-admin service
/// </summary>
public class AdminServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AdminServiceClient> _logger;
    private readonly ConfigurationService _configService;
    private bool _isInitialized = false;

    public AdminServiceClient(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AdminServiceClient> logger,
        ConfigurationService configService)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _configService = configService;
    }

    private const string AllTenantsGuid = "00000000-0000-0000-0000-000000000000";

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

            // Pass tenant context - don't send the "All Tenants" GUID as it's not a valid tenant
            var tenantId = httpContext.GetTenantId();
            if (!string.IsNullOrEmpty(tenantId) && tenantId != AllTenantsGuid)
            {
                request.Headers.Add("X-Tenant-ID", tenantId);
            }
        }
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
    /// Get all users from acl-admin service (filtered by tenant context or explicit tenantId)
    /// </summary>
    public async Task<List<AclUserResponse>> GetUsersAsync(Guid? tenantId = null)
    {
        await EnsureInitializedAsync();

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/user");
            AddAuthHeaders(request);

            // If a specific tenant is requested (from filter dropdown), override the tenant header
            if (tenantId.HasValue)
            {
                request.Headers.Remove("X-Tenant-ID");
                request.Headers.Add("X-Tenant-ID", tenantId.Value.ToString());
            }

            var response = await _httpClient.SendAsync(request);
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
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/user/{id}");
            AddAuthHeaders(request);

            var response = await _httpClient.SendAsync(request);
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
            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/user")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            AddAuthHeaders(request);

            var response = await _httpClient.SendAsync(request);
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
            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Put, $"/api/user/{id}")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            AddAuthHeaders(request);

            var response = await _httpClient.SendAsync(request);
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
            var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/user/{id}");
            AddAuthHeaders(request);

            var response = await _httpClient.SendAsync(request);
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

    #region Auth Methods

    /// <summary>
    /// Sync user from OAuth provider
    /// </summary>
    public async Task<SyncUserResponseDto?> SyncUserAsync(SyncUserRequestDto request)
    {
        await EnsureInitializedAsync();

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/sync", request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<SyncUserResponseDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing user in acl-admin");
            throw;
        }
    }

    /// <summary>
    /// Get user's tenants
    /// </summary>
    public async Task<List<TenantAccessDto>> GetUserTenantsAsync(Guid userId)
    {
        await EnsureInitializedAsync();

        try
        {
            var response = await _httpClient.GetAsync($"/api/auth/tenants/{userId}");
            response.EnsureSuccessStatusCode();

            var tenants = await response.Content.ReadFromJsonAsync<List<TenantAccessDto>>();
            return tenants ?? new List<TenantAccessDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user tenants from acl-admin");
            throw;
        }
    }

    #endregion

    #region Tenant Methods

    /// <summary>
    /// Get all tenants
    /// </summary>
    public async Task<List<TenantDto>> GetTenantsAsync(bool includeInactive = false)
    {
        await EnsureInitializedAsync();

        try
        {
            var response = await _httpClient.GetAsync($"/api/tenant?includeInactive={includeInactive}");
            response.EnsureSuccessStatusCode();

            var tenants = await response.Content.ReadFromJsonAsync<List<TenantDto>>();
            return tenants ?? new List<TenantDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tenants from acl-admin");
            throw;
        }
    }

    /// <summary>
    /// Get tenant by ID
    /// </summary>
    public async Task<TenantDto?> GetTenantAsync(Guid id)
    {
        await EnsureInitializedAsync();

        try
        {
            var response = await _httpClient.GetAsync($"/api/tenant/{id}");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<TenantDto>();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tenant from acl-admin");
            throw;
        }
    }

    /// <summary>
    /// Create a new tenant
    /// </summary>
    public async Task<TenantDto> CreateTenantAsync(CreateTenantRequest request)
    {
        await EnsureInitializedAsync();

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/tenant", request);
            response.EnsureSuccessStatusCode();

            var tenant = await response.Content.ReadFromJsonAsync<TenantDto>();
            return tenant ?? throw new Exception("Failed to deserialize tenant response");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            throw new InvalidOperationException("Tenant slug already exists");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant in acl-admin");
            throw;
        }
    }

    /// <summary>
    /// Update a tenant
    /// </summary>
    public async Task UpdateTenantAsync(Guid id, UpdateTenantRequest request)
    {
        await EnsureInitializedAsync();

        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/tenant/{id}", request);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException("Tenant not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant in acl-admin");
            throw;
        }
    }

    /// <summary>
    /// Delete a tenant
    /// </summary>
    public async Task DeleteTenantAsync(Guid id)
    {
        await EnsureInitializedAsync();

        try
        {
            var response = await _httpClient.DeleteAsync($"/api/tenant/{id}");
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException("Tenant not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tenant in acl-admin");
            throw;
        }
    }

    #endregion

    #region Invitation Methods

    /// <summary>
    /// Get all pending invitations for current tenant or specified tenant
    /// </summary>
    public async Task<List<AclInvitationResponse>> GetInvitationsAsync(Guid? tenantId = null)
    {
        await EnsureInitializedAsync();

        try
        {
            var url = "/api/invitation";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            AddAuthHeaders(request);

            // If a specific tenant is requested, override the tenant header
            if (tenantId.HasValue)
            {
                request.Headers.Remove("X-Tenant-ID");
                request.Headers.Add("X-Tenant-ID", tenantId.Value.ToString());
            }

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var invitations = await response.Content.ReadFromJsonAsync<List<AclInvitationResponse>>();
            return invitations ?? new List<AclInvitationResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching invitations from acl-admin");
            throw;
        }
    }

    /// <summary>
    /// Create a new invitation
    /// </summary>
    public async Task<AclInvitationResponse> CreateInvitationAsync(CreateInvitationRequest invitationRequest)
    {
        await EnsureInitializedAsync();

        try
        {
            var json = JsonSerializer.Serialize(new { email = invitationRequest.Email });
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/invitation")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            AddAuthHeaders(request);

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(errorContent.Contains("already been sent")
                    ? "An invitation has already been sent to this email address"
                    : "This user is already a member of this tenant");
            }

            response.EnsureSuccessStatusCode();

            var invitation = await response.Content.ReadFromJsonAsync<AclInvitationResponse>();
            return invitation ?? throw new Exception("Failed to deserialize invitation response");
        }
        catch (InvalidOperationException)
        {
            throw; // Re-throw conflict errors
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invitation in acl-admin");
            throw;
        }
    }

    /// <summary>
    /// Revoke an invitation
    /// </summary>
    public async Task RevokeInvitationAsync(Guid id)
    {
        await EnsureInitializedAsync();

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/invitation/{id}");
            AddAuthHeaders(request);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException("Invitation not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking invitation in acl-admin");
            throw;
        }
    }

    #endregion

    #region Onboarding Methods

    /// <summary>
    /// Get available subscription plans
    /// </summary>
    public async Task<List<SubscriptionPlanDto>> GetSubscriptionPlansAsync()
    {
        await EnsureInitializedAsync();

        try
        {
            var response = await _httpClient.GetAsync("/api/onboarding/plans");
            response.EnsureSuccessStatusCode();

            var plans = await response.Content.ReadFromJsonAsync<List<SubscriptionPlanDto>>();
            return plans ?? new List<SubscriptionPlanDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching subscription plans from acl-admin");
            throw;
        }
    }

    /// <summary>
    /// Get onboarding status for current user
    /// </summary>
    public async Task<OnboardingStatusDto?> GetOnboardingStatusAsync()
    {
        await EnsureInitializedAsync();

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/onboarding/status");
            AddAuthHeaders(request);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<OnboardingStatusDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching onboarding status from acl-admin");
            throw;
        }
    }

    /// <summary>
    /// Create tenant during onboarding
    /// </summary>
    public async Task<OnboardingTenantDto> CreateOnboardingTenantAsync(CreateOnboardingTenantRequest request)
    {
        await EnsureInitializedAsync();

        try
        {
            var json = JsonSerializer.Serialize(request);
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/onboarding/tenant")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            AddAuthHeaders(httpRequest);

            var response = await _httpClient.SendAsync(httpRequest);
            response.EnsureSuccessStatusCode();

            var tenant = await response.Content.ReadFromJsonAsync<OnboardingTenantDto>();
            return tenant ?? throw new Exception("Failed to deserialize tenant response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating onboarding tenant in acl-admin");
            throw;
        }
    }

    /// <summary>
    /// Create invitations during onboarding
    /// </summary>
    public async Task<OnboardingInvitationsDto> CreateOnboardingInvitationsAsync(CreateOnboardingInvitationsRequest request)
    {
        await EnsureInitializedAsync();

        try
        {
            var json = JsonSerializer.Serialize(request);
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/onboarding/invitations")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            AddAuthHeaders(httpRequest);

            var response = await _httpClient.SendAsync(httpRequest);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OnboardingInvitationsDto>();
            return result ?? throw new Exception("Failed to deserialize invitations response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating onboarding invitations in acl-admin");
            throw;
        }
    }

    /// <summary>
    /// Save payment method during onboarding
    /// </summary>
    public async Task SaveOnboardingPaymentAsync(Guid tenantId, string stripeCustomerId, string stripePaymentMethodId, string billingEmail)
    {
        await EnsureInitializedAsync();

        try
        {
            var payload = new
            {
                TenantId = tenantId,
                StripeCustomerId = stripeCustomerId,
                StripePaymentMethodId = stripePaymentMethodId,
                BillingEmail = billingEmail
            };

            var json = JsonSerializer.Serialize(payload);
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/onboarding/payment")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            AddAuthHeaders(httpRequest);

            var response = await _httpClient.SendAsync(httpRequest);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving onboarding payment in acl-admin");
            throw;
        }
    }

    /// <summary>
    /// Complete onboarding
    /// </summary>
    public async Task CompleteOnboardingAsync(Guid tenantId)
    {
        await EnsureInitializedAsync();

        try
        {
            var payload = new { TenantId = tenantId };
            var json = JsonSerializer.Serialize(payload);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/onboarding/complete")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            AddAuthHeaders(httpRequest);

            var response = await _httpClient.SendAsync(httpRequest);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing onboarding in acl-admin");
            throw;
        }
    }

    #endregion
}
