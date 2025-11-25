using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Fabrica.Domain.Esb.Interfaces;

namespace AdminDomainService.Models;

[Table("user", Schema = "fabrica")]
public class User : IOutboxEntity
{
    [Key]
    [Column("user_id")]
    public Guid UserId { get; set; } = Guid.NewGuid();

    // IOutboxEntity implementation - maps to UserId
    [NotMapped]
    [JsonIgnore]
    Guid IOutboxEntity.Id => UserId;

    // Users don't have a tenant_id directly, they can belong to multiple tenants via user_tenant
    // For outbox events, we'll use "system" as the default tenant
    [NotMapped]
    [JsonIgnore]
    string IOutboxEntity.TenantId => "system";

    [Required]
    [Column("email")]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Column("stytch_user_id")]
    [MaxLength(255)]
    public string? StytchUserId { get; set; }

    [Column("first_name")]
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [Column("last_name")]
    [MaxLength(100)]
    public string? LastName { get; set; }

    [Column("display_name")]
    [MaxLength(200)]
    public string? DisplayName { get; set; }

    [Required]
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Required]
    [Column("is_system_admin")]
    public bool IsSystemAdmin { get; set; } = false;

    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

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
}
