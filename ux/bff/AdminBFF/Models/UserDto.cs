namespace AdminBFF.Models;

/// <summary>
/// User DTO for list view
/// </summary>
public class UserDto
{
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystemAdmin { get; set; }
    public string? StytchUserId { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Detailed user DTO for single user view
/// </summary>
public class UserDetailDto
{
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystemAdmin { get; set; }
    public string? StytchUserId { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// User creation request DTO
/// </summary>
public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSystemAdmin { get; set; } = false;
}

/// <summary>
/// User update request DTO
/// </summary>
public class UpdateUserRequest
{
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsSystemAdmin { get; set; }
}

/// <summary>
/// User response from acl-admin service
/// </summary>
public class AclUserResponse
{
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystemAdmin { get; set; }
    public string? StytchUserId { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UserName
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

/// <summary>
/// User creation payload for acl-admin service
/// </summary>
public class AclCreateUserPayload
{
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystemAdmin { get; set; }
}

/// <summary>
/// User update payload for acl-admin service
/// </summary>
public class AclUpdateUserPayload
{
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsSystemAdmin { get; set; }
}
