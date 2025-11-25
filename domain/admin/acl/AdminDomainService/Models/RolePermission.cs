using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdminDomainService.Models;

[Table("role_permission", Schema = "fabrica")]
public class RolePermission
{
    [Key]
    [Column("role_permission_id")]
    public Guid RolePermissionId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("role_id")]
    public Guid RoleId { get; set; }

    [Required]
    [Column("permission_id")]
    public Guid PermissionId { get; set; }

    [Required]
    [Column("granted_at")]
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    [Column("granted_by")]
    public Guid? GrantedBy { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("RoleId")]
    public virtual Role? Role { get; set; }

    [ForeignKey("PermissionId")]
    public virtual Permission? Permission { get; set; }
}
