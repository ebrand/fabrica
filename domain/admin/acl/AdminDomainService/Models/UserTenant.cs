using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AdminDomainService.Models;

[Table("user_tenant", Schema = "fabrica")]
public class UserTenant
{
    [Key]
    [Column("user_tenant_id")]
    public Guid UserTenantId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Required]
    [Column("role")]
    [MaxLength(50)]
    public string Role { get; set; } = "viewer";

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
    [JsonIgnore]
    public virtual User? User { get; set; }

    [ForeignKey("TenantId")]
    [JsonIgnore]
    public virtual Tenant? Tenant { get; set; }
}
