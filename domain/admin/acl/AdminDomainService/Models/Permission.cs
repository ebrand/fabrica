using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdminDomainService.Models;

[Table("permission", Schema = "fabrica")]
public class Permission
{
    [Key]
    [Column("permission_id")]
    public Guid PermissionId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("permission_name")]
    [MaxLength(100)]
    public string PermissionName { get; set; } = string.Empty;

    [Column("permission_description")]
    public string? PermissionDescription { get; set; }

    [Required]
    [Column("resource")]
    [MaxLength(100)]
    public string Resource { get; set; } = string.Empty;

    [Required]
    [Column("action")]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("updated_by")]
    public Guid? UpdatedBy { get; set; }

    // Navigation properties
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
