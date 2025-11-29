using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AdminDomainService.Models;

[Table("tenant_subscription", Schema = "fabrica")]
public class TenantSubscription
{
    [Key]
    [Column("subscription_id")]
    public Guid SubscriptionId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Required]
    [Column("plan_id")]
    public Guid PlanId { get; set; }

    [Required]
    [Column("status")]
    [MaxLength(50)]
    public string Status { get; set; } = "active";

    [Column("stripe_customer_id")]
    [MaxLength(255)]
    public string? StripeCustomerId { get; set; }

    [Column("stripe_payment_method_id")]
    [MaxLength(255)]
    public string? StripePaymentMethodId { get; set; }

    [Column("billing_email")]
    [MaxLength(255)]
    public string? BillingEmail { get; set; }

    [Column("trial_ends_at")]
    public DateTime? TrialEndsAt { get; set; }

    [Required]
    [Column("current_period_start")]
    public DateTime CurrentPeriodStart { get; set; } = DateTime.UtcNow;

    [Column("current_period_end")]
    public DateTime? CurrentPeriodEnd { get; set; }

    [Column("canceled_at")]
    public DateTime? CanceledAt { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("TenantId")]
    [JsonIgnore]
    public virtual Tenant? Tenant { get; set; }

    [ForeignKey("PlanId")]
    [JsonIgnore]
    public virtual SubscriptionPlan? Plan { get; set; }
}
