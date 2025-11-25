using System.ComponentModel.DataAnnotations;

namespace AdminDomainService.Models;

public class CreateUserDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? DisplayName { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsSystemAdmin { get; set; } = false;
}
