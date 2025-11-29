using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdminDomainService.Data;
using AdminDomainService.Models;

namespace AdminDomainService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AdminDbContext _context;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AdminDbContext context, ILogger<AuthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Synchronizes a Stytch authenticated user with the local user database
    /// Creates or updates user record and returns user info with roles, permissions, and tenants
    /// </summary>
    [HttpPost("sync")]
    public async Task<ActionResult<SyncUserResponseDto>> SyncUser(SyncUserDto syncDto)
    {
        try
        {
            _logger.LogInformation("Syncing user with email: {Email}, Stytch ID: {StytchUserId}",
                syncDto.Email, syncDto.StytchUserId);

            // Check if user exists by email or Stytch ID
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == syncDto.Email || u.StytchUserId == syncDto.StytchUserId);

            bool isNewUser = false;

            if (existingUser == null)
            {
                // Create new user
                _logger.LogInformation("Creating new user for email: {Email}", syncDto.Email);

                existingUser = new User
                {
                    UserId = Guid.NewGuid(),
                    Email = syncDto.Email,
                    StytchUserId = syncDto.StytchUserId,
                    FirstName = syncDto.FirstName,
                    LastName = syncDto.LastName,
                    DisplayName = syncDto.DisplayName ?? $"{syncDto.FirstName} {syncDto.LastName}".Trim(),
                    IsActive = true,
                    IsSystemAdmin = false,
                    LastLoginAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(existingUser);
                isNewUser = true;

                // NOTE: Personal workspace creation removed - new users go through onboarding wizard
                _logger.LogInformation("New user {Email} will go through onboarding workflow", syncDto.Email);
            }
            else
            {
                // Update existing user
                _logger.LogInformation("Updating existing user: {UserId}, Email: {Email}",
                    existingUser.UserId, existingUser.Email);

                // Update Stytch ID if it wasn't set before
                if (string.IsNullOrEmpty(existingUser.StytchUserId))
                {
                    existingUser.StytchUserId = syncDto.StytchUserId;
                }

                // Update name fields if provided and not already set
                if (!string.IsNullOrEmpty(syncDto.FirstName) && string.IsNullOrEmpty(existingUser.FirstName))
                {
                    existingUser.FirstName = syncDto.FirstName;
                }

                if (!string.IsNullOrEmpty(syncDto.LastName) && string.IsNullOrEmpty(existingUser.LastName))
                {
                    existingUser.LastName = syncDto.LastName;
                }

                if (!string.IsNullOrEmpty(syncDto.DisplayName) && string.IsNullOrEmpty(existingUser.DisplayName))
                {
                    existingUser.DisplayName = syncDto.DisplayName;
                }

                // Update last login timestamp
                existingUser.LastLoginAt = DateTime.UtcNow;
                existingUser.UpdatedAt = DateTime.UtcNow;

                // NOTE: Personal workspace creation removed - users without tenants go through onboarding wizard
            }

            await _context.SaveChangesAsync();

            // Process any pending invitations for this user's email
            await ProcessPendingInvitationsAsync(existingUser);

            // Get user's roles and permissions
            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == existingUser.UserId && ur.IsActive)
                .Include(ur => ur.Role)
                .ToListAsync();

            var roleIds = userRoles.Select(ur => ur.RoleId).ToList();

            var permissions = await _context.RolePermissions
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Include(rp => rp.Permission)
                .Select(rp => rp.Permission!.PermissionName)
                .Distinct()
                .ToListAsync();

            // Get user's tenants
            var tenants = await GetUserTenantsAsync(existingUser.UserId);

            // For System Admins, prepend "All Tenants" option to allow cross-tenant access
            if (existingUser.IsSystemAdmin)
            {
                tenants.Insert(0, new TenantAccessDto
                {
                    TenantId = Guid.Empty, // Special GUID indicates "all tenants"
                    Name = "All Tenants",
                    Slug = "all",
                    Role = "system_admin",
                    IsPersonal = false
                });
            }

            // Determine if user needs onboarding (no tenants after processing invitations)
            var requiresOnboarding = !tenants.Any() && !existingUser.IsSystemAdmin;

            var response = new SyncUserResponseDto
            {
                UserId = existingUser.UserId,
                Email = existingUser.Email,
                StytchUserId = existingUser.StytchUserId,
                FirstName = existingUser.FirstName,
                LastName = existingUser.LastName,
                DisplayName = existingUser.DisplayName,
                AvatarMediaId = existingUser.AvatarMediaId,
                IsSystemAdmin = existingUser.IsSystemAdmin,
                IsNewUser = isNewUser,
                RequiresOnboarding = requiresOnboarding,
                Roles = userRoles.Select(ur => ur.Role!.RoleName).ToList(),
                Permissions = permissions,
                Tenants = tenants
            };

            _logger.LogInformation("Successfully synced user: {UserId}, IsNew: {IsNewUser}, RequiresOnboarding: {RequiresOnboarding}, Roles: {RoleCount}, Tenants: {TenantCount}",
                existingUser.UserId, isNewUser, requiresOnboarding, response.Roles.Count, response.Tenants.Count);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing user with email: {Email}", syncDto.Email);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets all tenants a user has access to
    /// </summary>
    [HttpGet("tenants/{userId}")]
    public async Task<ActionResult<List<TenantAccessDto>>> GetUserTenants(Guid userId)
    {
        try
        {
            // Check if user is a system admin
            var user = await _context.Users.FindAsync(userId);
            var tenants = await GetUserTenantsAsync(userId);

            // For System Admins, prepend "All Tenants" option
            if (user?.IsSystemAdmin == true)
            {
                tenants.Insert(0, new TenantAccessDto
                {
                    TenantId = Guid.Empty,
                    Name = "All Tenants",
                    Slug = "all",
                    Role = "system_admin",
                    IsPersonal = false
                });
            }

            return Ok(tenants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenants for user: {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    private async Task<List<TenantAccessDto>> GetUserTenantsAsync(Guid userId)
    {
        return await _context.UserTenants
            .Where(ut => ut.UserId == userId && ut.IsActive)
            .Include(ut => ut.Tenant)
            .Where(ut => ut.Tenant != null && ut.Tenant.IsActive)
            .Select(ut => new TenantAccessDto
            {
                TenantId = ut.TenantId,
                Name = ut.Tenant!.Name,
                Slug = ut.Tenant.Slug,
                Role = ut.Role,
                IsPersonal = ut.Tenant.IsPersonal
            })
            .ToListAsync();
    }

    private async Task<Tenant> CreatePersonalTenantAsync(User user)
    {
        var displayName = user.DisplayName ?? user.FirstName ?? user.Email.Split('@')[0];
        var slugBase = GenerateSlug(user.Email.Split('@')[0]);
        var slug = await EnsureUniqueSlugAsync(slugBase);

        var tenant = new Tenant
        {
            TenantId = Guid.NewGuid(),
            Name = $"{displayName}'s Workspace",
            Slug = slug,
            IsPersonal = true,
            IsActive = true,
            OwnerUserId = user.UserId,
            CreatedBy = user.UserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Tenants.Add(tenant);

        // Link user to their personal tenant as owner
        var userTenant = new UserTenant
        {
            UserTenantId = Guid.NewGuid(),
            UserId = user.UserId,
            TenantId = tenant.TenantId,
            Role = "owner",
            IsActive = true,
            GrantedBy = user.UserId,
            GrantedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.UserTenants.Add(userTenant);

        return tenant;
    }

    private static string GenerateSlug(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "workspace";

        // Convert to lowercase and replace non-alphanumeric chars with hyphens
        var slug = Regex.Replace(input.ToLowerInvariant(), @"[^a-z0-9]+", "-");
        // Remove leading/trailing hyphens
        slug = slug.Trim('-');
        // Limit length
        if (slug.Length > 50)
            slug = slug.Substring(0, 50).TrimEnd('-');

        return string.IsNullOrEmpty(slug) ? "workspace" : slug;
    }

    private async Task<string> EnsureUniqueSlugAsync(string baseSlug)
    {
        var slug = baseSlug;
        var counter = 0;

        while (await _context.Tenants.AnyAsync(t => t.Slug == slug))
        {
            counter++;
            slug = $"{baseSlug}-{counter}";
        }

        return slug;
    }

    /// <summary>
    /// Gets user permissions by user ID
    /// </summary>
    [HttpGet("permissions/{userId}")]
    public async Task<ActionResult<List<string>>> GetUserPermissions(Guid userId)
    {
        try
        {
            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == userId && ur.IsActive)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            var permissions = await _context.RolePermissions
                .Where(rp => userRoles.Contains(rp.RoleId))
                .Include(rp => rp.Permission)
                .Where(rp => rp.Permission!.IsActive)
                .Select(rp => rp.Permission!.PermissionName)
                .Distinct()
                .ToListAsync();

            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions for user: {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Processes pending invitations for a user based on their email address.
    /// When a user logs in, any pending invitations matching their email are automatically accepted,
    /// adding the user to the inviting tenant as a member.
    /// </summary>
    private async Task ProcessPendingInvitationsAsync(User user)
    {
        try
        {
            var pendingInvitations = await _context.Invitations
                .Where(i => i.Email.ToLower() == user.Email.ToLower()
                    && i.Status == "pending"
                    && i.ExpiresAt > DateTime.UtcNow)
                .Include(i => i.Tenant)
                .ToListAsync();

            if (pendingInvitations.Count == 0)
            {
                return;
            }

            _logger.LogInformation("Found {Count} pending invitations for user {Email}",
                pendingInvitations.Count, user.Email);

            foreach (var invitation in pendingInvitations)
            {
                // Check if user is already in this tenant
                var existingMembership = await _context.UserTenants
                    .FirstOrDefaultAsync(ut => ut.UserId == user.UserId
                        && ut.TenantId == invitation.TenantId);

                if (existingMembership == null)
                {
                    // Add user to tenant as member
                    var userTenant = new UserTenant
                    {
                        UserTenantId = Guid.NewGuid(),
                        UserId = user.UserId,
                        TenantId = invitation.TenantId,
                        Role = "member",
                        IsActive = true,
                        GrantedBy = invitation.InvitedBy,
                        GrantedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.UserTenants.Add(userTenant);

                    _logger.LogInformation(
                        "User {Email} accepted invitation to tenant {TenantName} ({TenantId})",
                        user.Email, invitation.Tenant?.Name, invitation.TenantId);
                }
                else
                {
                    _logger.LogInformation(
                        "User {Email} already a member of tenant {TenantId}, marking invitation as accepted",
                        user.Email, invitation.TenantId);
                }

                // Mark invitation as accepted
                invitation.Status = "accepted";
                invitation.AcceptedAt = DateTime.UtcNow;
                invitation.AcceptedByUserId = user.UserId;
                invitation.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Processed {Count} invitations for user {Email}",
                pendingInvitations.Count, user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending invitations for user {Email}", user.Email);
            // Don't fail the login if invitation processing fails
        }
    }
}
