using AdminDomainService.Data;
using AdminDomainService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdminDomainService.Controllers;

[ApiController]
[Route("api/user")]
public class UserController : ControllerBase
{
    private readonly AdminDbContext _context;
    private readonly ILogger<UserController> _logger;

    public UserController(AdminDbContext context, ILogger<UserController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Checks if the caller is a System Admin via the X-Is-System-Admin header
    /// </summary>
    private bool IsCallerSystemAdmin()
    {
        return Request.Headers.TryGetValue("X-Is-System-Admin", out var value)
            && value.FirstOrDefault()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Gets the tenant ID from the X-Tenant-ID header
    /// Returns null if not provided or if empty GUID (All Tenants mode)
    /// </summary>
    private Guid? GetTenantIdFromHeader()
    {
        if (Request.Headers.TryGetValue("X-Tenant-ID", out var value))
        {
            var tenantIdString = value.FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantIdString) && Guid.TryParse(tenantIdString, out var tenantId))
            {
                // Empty GUID means "All Tenants" mode - return null to skip filtering
                if (tenantId == Guid.Empty)
                {
                    return null;
                }
                return tenantId;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets the caller's user ID from the X-User-ID header
    /// </summary>
    private Guid? GetCallerUserId()
    {
        if (Request.Headers.TryGetValue("X-User-ID", out var value))
        {
            var userIdString = value.FirstOrDefault();
            if (!string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out var userId))
            {
                return userId;
            }
        }
        return null;
    }

    /// <summary>
    /// Checks if the caller is the owner of the specified tenant
    /// </summary>
    private async Task<bool> IsCallerTenantOwnerAsync(Guid tenantId)
    {
        var callerId = GetCallerUserId();
        if (!callerId.HasValue)
        {
            return false;
        }

        var userTenant = await _context.UserTenants
            .FirstOrDefaultAsync(ut => ut.UserId == callerId.Value
                && ut.TenantId == tenantId
                && ut.IsActive);

        return userTenant?.Role == "owner";
    }

    /// <summary>
    /// Checks if the caller can manage users in the current tenant context
    /// Returns true if: System Admin, or tenant owner
    /// </summary>
    private async Task<bool> CanCallerManageUsersAsync()
    {
        if (IsCallerSystemAdmin())
        {
            return true;
        }

        var tenantId = GetTenantIdFromHeader();
        if (tenantId.HasValue)
        {
            return await IsCallerTenantOwnerAsync(tenantId.Value);
        }

        return false;
    }

    // GET: api/users
    [HttpGet]
    public async Task<ActionResult> GetUsers()
    {
        try
        {
            var tenantId = GetTenantIdFromHeader();
            var isSystemAdmin = IsCallerSystemAdmin();

            // If tenant ID is specified, filter by users in that tenant and include their role
            if (tenantId.HasValue)
            {
                _logger.LogInformation("Filtering users by tenant {TenantId}", tenantId.Value);

                var usersWithRoles = await _context.UserTenants
                    .Where(ut => ut.TenantId == tenantId.Value && ut.IsActive)
                    .Include(ut => ut.User)
                    .Where(ut => ut.User != null)
                    .OrderByDescending(ut => ut.User!.CreatedAt)
                    .Select(ut => new
                    {
                        ut.User!.UserId,
                        ut.User.Email,
                        ut.User.StytchUserId,
                        ut.User.FirstName,
                        ut.User.LastName,
                        ut.User.DisplayName,
                        ut.User.AvatarMediaId,
                        ut.User.IsActive,
                        ut.User.IsSystemAdmin,
                        ut.User.LastLoginAt,
                        ut.User.CreatedAt,
                        ut.User.UpdatedAt,
                        TenantRole = ut.Role // Include the user's role in this tenant
                    })
                    .ToListAsync();

                return Ok(usersWithRoles);
            }

            // If no tenant ID (All Tenants mode) and user is System Admin, return all users
            // If no tenant ID and not System Admin, return empty (security: deny by default)
            if (!isSystemAdmin)
            {
                _logger.LogWarning("Non-admin user requested users without tenant context - returning empty");
                return Ok(new List<object>());
            }

            _logger.LogInformation("System Admin viewing all users (All Tenants mode)");

            // For All Tenants mode, return users with their tenant memberships
            var users = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new
                {
                    u.UserId,
                    u.Email,
                    u.StytchUserId,
                    u.FirstName,
                    u.LastName,
                    u.DisplayName,
                    u.AvatarMediaId,
                    u.IsActive,
                    u.IsSystemAdmin,
                    u.LastLoginAt,
                    u.CreatedAt,
                    u.UpdatedAt,
                    TenantRole = (string?)null, // No specific tenant role in All Tenants mode
                    Tenants = _context.UserTenants
                        .Where(ut => ut.UserId == u.UserId && ut.IsActive)
                        .Join(_context.Tenants.Where(t => t.IsActive),
                            ut => ut.TenantId,
                            t => t.TenantId,
                            (ut, t) => new { t.Name, ut.Role })
                        .ToList()
                })
                .ToListAsync();

            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching users");
            return BadRequest(new { error = ex.Message });
        }
    }

    // GET: api/users/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(Guid id)
    {
        try
        {
            var tenantId = GetTenantIdFromHeader();
            var isSystemAdmin = IsCallerSystemAdmin();

            var user = await _context.Users
                .Include(u => u.UserTenants)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            // Verify tenant access unless System Admin in All Tenants mode
            if (tenantId.HasValue)
            {
                if (!user.UserTenants.Any(ut => ut.TenantId == tenantId.Value && ut.IsActive))
                {
                    _logger.LogWarning("User {UserId} not found in tenant {TenantId}", id, tenantId.Value);
                    return NotFound(new { error = "User not found" });
                }
            }
            else if (!isSystemAdmin)
            {
                _logger.LogWarning("Non-admin user requested user {UserId} without tenant context", id);
                return NotFound(new { error = "User not found" });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user {UserId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    // POST: api/users
    // Note: Direct user creation is restricted to System Admins only.
    // Tenant owners should use the invitation system (POST /api/invitation) to add users.
    [HttpPost]
    public async Task<ActionResult<User>> CreateUser(CreateUserDto dto)
    {
        try
        {
            // Authorization: Only System Admin can create users directly
            // Tenant owners must use the invitation system instead
            if (!IsCallerSystemAdmin())
            {
                _logger.LogWarning("Non-admin user creation attempt by {CallerId}. Direct user creation requires System Admin. Use invitations instead.", GetCallerUserId());
                return StatusCode(403, new { error = "Direct user creation requires System Admin privileges. Please use the invitation system to add users to your tenant." });
            }

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            {
                return Conflict(new { error = "Email already exists" });
            }

            // Defense in depth: Only allow IsSystemAdmin=true if caller is System Admin
            var isSystemAdmin = dto.IsSystemAdmin;
            if (isSystemAdmin && !IsCallerSystemAdmin())
            {
                _logger.LogWarning("Non-admin caller attempted to create user {Email} with IsSystemAdmin=true. Rejecting flag.",
                    dto.Email);
                isSystemAdmin = false;
            }

            var user = new User
            {
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                DisplayName = dto.DisplayName ?? $"{dto.FirstName} {dto.LastName}".Trim(),
                AvatarMediaId = dto.AvatarMediaId,
                IsActive = dto.IsActive,
                IsSystemAdmin = isSystemAdmin,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);

            // If a tenant context is specified, add the new user to that tenant as a member
            var tenantId = GetTenantIdFromHeader();
            if (tenantId.HasValue)
            {
                var userTenant = new UserTenant
                {
                    UserTenantId = Guid.NewGuid(),
                    UserId = user.UserId,
                    TenantId = tenantId.Value,
                    Role = "member",
                    IsActive = true,
                    GrantedBy = GetCallerUserId(),
                    GrantedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.UserTenants.Add(userTenant);
                _logger.LogInformation("Added new user {UserId} to tenant {TenantId} as member", user.UserId, tenantId.Value);
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return BadRequest(new { error = ex.Message });
        }
    }

    // PUT: api/users/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<User>> UpdateUser(Guid id, UpdateUserDto dto)
    {
        try
        {
            var callerId = GetCallerUserId();
            var isEditingSelf = callerId.HasValue && callerId.Value == id;
            var canManageUsers = await CanCallerManageUsersAsync();

            // Authorization: Users can edit their own profile, or System Admin/tenant owner can edit anyone
            if (!isEditingSelf && !canManageUsers)
            {
                _logger.LogWarning("Unauthorized user update attempt: {CallerId} tried to update {TargetUserId}", callerId, id);
                return StatusCode(403, new { error = "You do not have permission to update this user" });
            }

            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            // Check if email is being changed and if it already exists
            if (dto.Email != null && dto.Email != user.Email)
            {
                if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                {
                    return Conflict(new { error = "Email already exists" });
                }
                user.Email = dto.Email;
            }

            // Update only provided fields
            if (dto.FirstName != null) user.FirstName = dto.FirstName;
            if (dto.LastName != null) user.LastName = dto.LastName;
            if (dto.DisplayName != null) user.DisplayName = dto.DisplayName;
            if (dto.AvatarMediaId.HasValue) user.AvatarMediaId = dto.AvatarMediaId;

            // IsActive can only be changed by System Admin or tenant owner (not by user editing themselves)
            if (dto.IsActive.HasValue)
            {
                if (canManageUsers)
                {
                    user.IsActive = dto.IsActive.Value;
                }
                else
                {
                    _logger.LogWarning("Non-admin caller attempted to modify IsActive for user {UserId}. Ignoring change.", id);
                }
            }

            // Defense in depth: Only allow IsSystemAdmin changes if caller is System Admin
            if (dto.IsSystemAdmin.HasValue)
            {
                if (IsCallerSystemAdmin())
                {
                    user.IsSystemAdmin = dto.IsSystemAdmin.Value;
                }
                else
                {
                    _logger.LogWarning("Non-admin caller attempted to modify IsSystemAdmin for user {UserId}. Ignoring change.", id);
                    // Don't update IsSystemAdmin - silently ignore the change
                }
            }

            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    // DELETE: api/users/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        try
        {
            // Authorization: Only System Admin or tenant owner can delete users
            if (!await CanCallerManageUsersAsync())
            {
                _logger.LogWarning("Unauthorized user deletion attempt by {CallerId} for user {TargetUserId}",
                    GetCallerUserId(), id);
                return StatusCode(403, new { error = "You do not have permission to delete users" });
            }

            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User deleted successfully", id = user.UserId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }
}
