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
    /// Creates or updates user record and returns user info with roles and permissions
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

                // Assign default Viewer role to new users
                var viewerRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.RoleName == "Viewer" && r.IsActive);

                if (viewerRole != null)
                {
                    var userRole = new UserRole
                    {
                        UserRoleId = Guid.NewGuid(),
                        UserId = existingUser.UserId,
                        RoleId = viewerRole.RoleId,
                        TenantId = null,
                        IsActive = true,
                        GrantedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.UserRoles.Add(userRole);
                    _logger.LogInformation("Assigned Viewer role to new user: {Email}", syncDto.Email);
                }
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
            }

            await _context.SaveChangesAsync();

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

            var response = new SyncUserResponseDto
            {
                UserId = existingUser.UserId,
                Email = existingUser.Email,
                StytchUserId = existingUser.StytchUserId,
                FirstName = existingUser.FirstName,
                LastName = existingUser.LastName,
                DisplayName = existingUser.DisplayName,
                IsNewUser = isNewUser,
                Roles = userRoles.Select(ur => ur.Role!.RoleName).ToList(),
                Permissions = permissions
            };

            _logger.LogInformation("Successfully synced user: {UserId}, IsNew: {IsNewUser}, Roles: {RoleCount}",
                existingUser.UserId, isNewUser, response.Roles.Count);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing user with email: {Email}", syncDto.Email);
            return BadRequest(new { error = ex.Message });
        }
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
}
