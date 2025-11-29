using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AdminDomainService.Models;

[Table("subscription_plan", Schema = "fabrica")]
public class SubscriptionPlan
{
    [Key]
    [Column("plan_id")]
    public Guid PlanId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [Column("price_cents")]
    public int PriceCents { get; set; }

    [Required]
    [Column("billing_interval")]
    [MaxLength(20)]
    public string BillingInterval { get; set; } = "monthly";

    [Required]
    [Column("max_users")]
    public int MaxUsers { get; set; } = 5;

    [Required]
    [Column("max_products")]
    public int MaxProducts { get; set; } = 100;

    [Required]
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Required]
    [Column("display_order")]
    public int DisplayOrder { get; set; } = 0;

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [JsonIgnore]
    public virtual ICollection<TenantSubscription> TenantSubscriptions { get; set; } = new List<TenantSubscription>();
}
