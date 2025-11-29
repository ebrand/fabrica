using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Fabrica.Domain.Esb.Interfaces;

namespace AdminDomainService.Models;

[Table("tenant", Schema = "fabrica")]
public class Tenant : IOutboxEntity
{
    [Key]
    [Column("tenant_id")]
    public Guid TenantId { get; set; } = Guid.NewGuid();

    // IOutboxEntity implementation
    [NotMapped]
    [JsonIgnore]
    Guid IOutboxEntity.Id => TenantId;

    [NotMapped]
    [JsonIgnore]
    string IOutboxEntity.TenantId => TenantId.ToString();

    [Required]
    [Column("name")]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column("slug")]
    [MaxLength(100)]
    public string Slug { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("logo_media_id")]
    public Guid? LogoMediaId { get; set; }

    [Required]
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Required]
    [Column("is_personal")]
    public bool IsPersonal { get; set; } = false;

    [Column("owner_user_id")]
    public Guid? OwnerUserId { get; set; }

    [Column("settings", TypeName = "jsonb")]
    public string Settings { get; set; } = "{}";

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

    [Required]
    [Column("onboarding_completed")]
    public bool OnboardingCompleted { get; set; } = false;

    [Required]
    [Column("onboarding_step")]
    public int OnboardingStep { get; set; } = 0;

    // Navigation properties
    [ForeignKey("OwnerUserId")]
    [JsonIgnore]
    public virtual User? Owner { get; set; }

    [JsonIgnore]
    public virtual ICollection<UserTenant> UserTenants { get; set; } = new List<UserTenant>();
}
