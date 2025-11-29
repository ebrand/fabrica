using System.ComponentModel.DataAnnotations;

namespace AdminDomainService.Models;

public class UpdateUserDto
{
    [EmailAddress]
    public string? Email { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? DisplayName { get; set; }

    public Guid? AvatarMediaId { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsSystemAdmin { get; set; }
}
