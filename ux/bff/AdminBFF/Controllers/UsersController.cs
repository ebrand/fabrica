using AdminBFF.Models;
using AdminBFF.Middleware;
using AdminBFF.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdminBFF.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AdminServiceClient _adminClient;
    private readonly ILogger<UsersController> _logger;

    public UsersController(AdminServiceClient adminClient, ILogger<UsersController> logger)
    {
        _adminClient = adminClient;
        _logger = logger;
    }

    /// <summary>
    /// Get all users, optionally filtered by tenant
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetUsers([FromQuery] Guid? tenantId = null)
    {
        try
        {
            var aclUsers = await _adminClient.GetUsersAsync(tenantId);

            // Transform the response to match the expected format
            var users = aclUsers.Select(user => new UserDto
            {
                UserId        = user.UserId,
                Email         = user.Email,
                DisplayName   = user.DisplayName ?? $"{user.FirstName ?? ""} {user.LastName ?? ""}".Trim(),
                FirstName     = user.FirstName,
                LastName      = user.LastName,
                AvatarMediaId = user.AvatarMediaId,
                IsActive      = user.IsActive,
                IsSystemAdmin = user.IsSystemAdmin,
                StytchUserId  = user.StytchUserId,
                LastLoginAt   = user.LastLoginAt,
                CreatedAt     = user.CreatedAt,
                UpdatedAt     = user.UpdatedAt,
                TenantRole    = user.TenantRole,
                Tenants       = user.Tenants
            }).ToList();

            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching users from acl-admin");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get single user
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDetailDto>> GetUser(string id)
    {
        try
        {
            var user = await _adminClient.GetUserAsync(id);

            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            var userDto = new UserDetailDto
            {
                UserId        = user.UserId,
                Email         = user.Email,
                FirstName     = user.FirstName,
                LastName      = user.LastName,
                DisplayName   = user.DisplayName,
                AvatarMediaId = user.AvatarMediaId,
                IsActive      = user.IsActive,
                IsSystemAdmin = user.IsSystemAdmin,
                StytchUserId  = user.StytchUserId,
                LastLoginAt   = user.LastLoginAt,
                CreatedAt     = user.CreatedAt,
                UpdatedAt     = user.UpdatedAt
            };

            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user from acl-admin");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Create new user
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserDetailDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                return BadRequest(new { error = "Email is required" });
            }

            // Only System Admins can set the IsSystemAdmin field on new users
            var isCurrentUserAdmin = HttpContext.IsCurrentUserSystemAdmin();
            var requestedIsSystemAdmin = request.IsSystemAdmin;

            if (requestedIsSystemAdmin == true && !isCurrentUserAdmin)
            {
                _logger.LogWarning(
                    "Non-admin user {UserId} attempted to create user {Email} with IsSystemAdmin=true. Stripping flag.",
                    HttpContext.GetUserId(), request.Email);
                requestedIsSystemAdmin = false;
            }

            var payload = new AclCreateUserPayload
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                DisplayName = request.DisplayName ??
                              $"{request.FirstName} {request.LastName}".Trim(),
                AvatarMediaId = request.AvatarMediaId,
                IsActive = request.IsActive,
                IsSystemAdmin = requestedIsSystemAdmin
            };

            var user = await _adminClient.CreateUserAsync(payload);

            var userDto = new UserDetailDto
            {
                UserId        = user.UserId,
                Email         = user.Email,
                FirstName     = user.FirstName,
                LastName      = user.LastName,
                DisplayName   = user.DisplayName,
                AvatarMediaId = user.AvatarMediaId,
                IsActive      = user.IsActive,
                IsSystemAdmin = user.IsSystemAdmin,
                CreatedAt     = user.CreatedAt,
                UpdatedAt     = user.UpdatedAt
            };

            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, userDto);
        }
        catch (InvalidOperationException ex) when (ex.Message == "Email already exists")
        {
            _logger.LogWarning(ex, "Email already exists");
            return Conflict(new { error = "Email already exists" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user in acl-admin");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update user
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<UserDetailDto>> UpdateUser(string id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            // Only System Admins can modify the IsSystemAdmin field
            var isCurrentUserAdmin = HttpContext.IsCurrentUserSystemAdmin();
            var requestedIsSystemAdmin = request.IsSystemAdmin;

            // If the request tries to change IsSystemAdmin but requester is not a System Admin, strip it
            if (requestedIsSystemAdmin.HasValue && !isCurrentUserAdmin)
            {
                _logger.LogWarning(
                    "Non-admin user {UserId} attempted to modify IsSystemAdmin for user {TargetUserId}. Stripping IsSystemAdmin from request.",
                    HttpContext.GetUserId(), id);
                requestedIsSystemAdmin = null; // Don't change IsSystemAdmin
            }

            var payload = new AclUpdateUserPayload
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                DisplayName = request.DisplayName,
                AvatarMediaId = request.AvatarMediaId,
                IsActive = request.IsActive,
                IsSystemAdmin = requestedIsSystemAdmin
            };

            var user = await _adminClient.UpdateUserAsync(id, payload);

            var userDto = new UserDetailDto
            {
                UserId        = user.UserId,
                Email         = user.Email,
                FirstName     = user.FirstName,
                LastName      = user.LastName,
                DisplayName   = user.DisplayName,
                AvatarMediaId = user.AvatarMediaId,
                IsActive      = user.IsActive,
                IsSystemAdmin = user.IsSystemAdmin,
                StytchUserId  = user.StytchUserId,
                LastLoginAt   = user.LastLoginAt,
                CreatedAt     = user.CreatedAt,
                UpdatedAt     = user.UpdatedAt
            };

            return Ok(userDto);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found");
            return NotFound(new { error = "User not found" });
        }
        catch (InvalidOperationException ex) when (ex.Message == "Email already exists")
        {
            _logger.LogWarning(ex, "Email already exists");
            return Conflict(new { error = "Email already exists" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user in acl-admin");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete user
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteUser(string id)
    {
        try
        {
            await _adminClient.DeleteUserAsync(id);
            return Ok(new { message = "User deleted successfully", id });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found");
            return NotFound(new { error = "User not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user in acl-admin");
            return BadRequest(new { error = ex.Message });
        }
    }
}
