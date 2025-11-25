using AdminBFF.Models;
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
    /// Get all users
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetUsers()
    {
        try
        {
            var aclUsers = await _adminClient.GetUsersAsync();

            // Transform the response to match the expected format
            var users = aclUsers.Select(user => new UserDto
            {
                UserId        = user.UserId,
                Email         = user.Email,
                DisplayName   = user.DisplayName ?? $"{user.FirstName ?? ""} {user.LastName ?? ""}".Trim(),
                FirstName     = user.FirstName,
                LastName      = user.LastName,
                IsActive      = user.IsActive,
                IsSystemAdmin = user.IsSystemAdmin,
                StytchUserId  = user.StytchUserId,
                LastLoginAt   = user.LastLoginAt,
                CreatedAt     = user.CreatedAt,
                UpdatedAt     = user.UpdatedAt
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

            var payload = new AclCreateUserPayload
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                DisplayName = request.DisplayName ??
                              $"{request.FirstName} {request.LastName}".Trim(),
                IsActive = request.IsActive,
                IsSystemAdmin = request.IsSystemAdmin
            };

            var user = await _adminClient.CreateUserAsync(payload);

            var userDto = new UserDetailDto
            {
                UserId        = user.UserId,
                Email         = user.Email,
                FirstName     = user.FirstName,
                LastName      = user.LastName,
                DisplayName   = user.DisplayName,
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
            var payload = new AclUpdateUserPayload
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                DisplayName = request.DisplayName,
                IsActive = request.IsActive,
                IsSystemAdmin = request.IsSystemAdmin
            };

            var user = await _adminClient.UpdateUserAsync(id, payload);

            var userDto = new UserDetailDto
            {
                UserId        = user.UserId,
                Email         = user.Email,
                FirstName     = user.FirstName,
                LastName      = user.LastName,
                DisplayName   = user.DisplayName,
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
