namespace AdminBFF.Middleware;

/// <summary>
/// Middleware that extracts tenant context from cookies or headers
/// and makes it available through HttpContext.Items
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;
    private const string TenantCookieName = "X-Tenant-ID";
    private const string TenantHeaderName = "X-Tenant-ID";
    private const string TenantItemKey = "TenantId";
    private const string AllTenantsItemKey = "IsAllTenantsMode";
    private const string UserIdCookieName = "X-User-ID";
    private const string IsSystemAdminCookieName = "X-Is-System-Admin";
    private const string UserIdItemKey = "UserId";
    private const string IsSystemAdminItemKey = "IsSystemAdmin";
    // Empty GUID indicates "All Tenants" mode for System Admins
    private const string AllTenantsGuid = "00000000-0000-0000-0000-000000000000";

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Try to get tenant from cookie first, then header
        var tenantId = context.Request.Cookies[TenantCookieName]
            ?? context.Request.Headers[TenantHeaderName].FirstOrDefault();

        if (!string.IsNullOrEmpty(tenantId))
        {
            context.Items[TenantItemKey] = tenantId;

            // Check if this is "All Tenants" mode (System Admin cross-tenant access)
            var isAllTenantsMode = tenantId == AllTenantsGuid;
            context.Items[AllTenantsItemKey] = isAllTenantsMode;

            _logger.LogDebug("Tenant context set: {TenantId}, AllTenantsMode: {IsAllTenantsMode}",
                tenantId, isAllTenantsMode);
        }

        // Extract user context from cookies or headers
        var userId = context.Request.Cookies[UserIdCookieName]
            ?? context.Request.Headers[UserIdCookieName].FirstOrDefault();
        var isSystemAdminCookie = context.Request.Cookies[IsSystemAdminCookieName]
            ?? context.Request.Headers[IsSystemAdminCookieName].FirstOrDefault();

        if (!string.IsNullOrEmpty(userId))
        {
            context.Items[UserIdItemKey] = userId;
        }

        if (!string.IsNullOrEmpty(isSystemAdminCookie))
        {
            var isSystemAdmin = isSystemAdminCookie.Equals("true", StringComparison.OrdinalIgnoreCase);
            context.Items[IsSystemAdminItemKey] = isSystemAdmin;
            _logger.LogDebug("User context set: {UserId}, IsSystemAdmin: {IsSystemAdmin}",
                userId, isSystemAdmin);
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for TenantMiddleware
/// </summary>
public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantMiddleware>();
    }

    /// <summary>
    /// Gets the current tenant ID from HttpContext
    /// </summary>
    public static string? GetTenantId(this HttpContext context)
    {
        return context.Items["TenantId"]?.ToString();
    }

    /// <summary>
    /// Gets the current tenant ID or throws if not set
    /// </summary>
    public static string GetRequiredTenantId(this HttpContext context)
    {
        var tenantId = context.GetTenantId();
        if (string.IsNullOrEmpty(tenantId))
        {
            throw new InvalidOperationException("Tenant context is required but not set");
        }
        return tenantId;
    }

    /// <summary>
    /// Checks if "All Tenants" mode is active (System Admin cross-tenant access)
    /// </summary>
    public static bool IsAllTenantsMode(this HttpContext context)
    {
        return context.Items["IsAllTenantsMode"] as bool? ?? false;
    }

    /// <summary>
    /// Gets the tenant ID for filtering, or null if in "All Tenants" mode
    /// </summary>
    public static string? GetTenantIdForFilter(this HttpContext context)
    {
        if (context.IsAllTenantsMode())
        {
            return null; // No filtering for System Admins in "All Tenants" mode
        }
        return context.GetTenantId();
    }

    /// <summary>
    /// Gets the current user ID from HttpContext
    /// </summary>
    public static string? GetUserId(this HttpContext context)
    {
        return context.Items["UserId"]?.ToString();
    }

    /// <summary>
    /// Checks if the current user is a System Admin
    /// </summary>
    public static bool IsCurrentUserSystemAdmin(this HttpContext context)
    {
        return context.Items["IsSystemAdmin"] as bool? ?? false;
    }
}
