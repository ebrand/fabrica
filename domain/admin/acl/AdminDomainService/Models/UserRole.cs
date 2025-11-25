using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdminDomainService.Models;

[Table("user_role", Schema = "fabrica")]
public class UserRole
{
    [Key]
    [Column("user_role_id")]
    public Guid UserRoleId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("role_id")]
    public Guid RoleId { get; set; }

    [Column("tenant_id")]
    [MaxLength(100)]
    public string? TenantId { get; set; }

    [Required]
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Required]
    [Column("granted_at")]
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    [Column("granted_by")]
    public Guid? GrantedBy { get; set; }

    [Column("revoked_at")]
    public DateTime? RevokedAt { get; set; }

    [Column("revoked_by")]
    public Guid? RevokedBy { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    [ForeignKey("RoleId")]
    public virtual Role? Role { get; set; }
}
