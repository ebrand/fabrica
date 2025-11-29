using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AdminDomainService.Models;

[Table("invitation", Schema = "fabrica")]
public class Invitation
{
    [Key]
    [Column("invitation_id")]
    public Guid InvitationId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("email")]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Required]
    [Column("invited_by")]
    public Guid InvitedBy { get; set; }

    [Required]
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    [Required]
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("accepted_at")]
    public DateTime? AcceptedAt { get; set; }

    [Column("accepted_by_user_id")]
    public Guid? AcceptedByUserId { get; set; }

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

    [ForeignKey("InvitedBy")]
    [JsonIgnore]
    public virtual User? InvitedByUser { get; set; }

    [ForeignKey("AcceptedByUserId")]
    [JsonIgnore]
    public virtual User? AcceptedByUser { get; set; }
}
