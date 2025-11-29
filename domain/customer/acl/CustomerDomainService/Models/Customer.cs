using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Fabrica.Domain.Esb.Interfaces;

namespace CustomerDomainService.Models;

[Table("customer", Schema = "fabrica")]
public class Customer : IOutboxEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("tenant_id")]
    [MaxLength(100)]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    [Column("email")]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Column("first_name")]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Column("last_name")]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Column("display_name")]
    [MaxLength(255)]
    public string? DisplayName { get; set; }

    [Column("phone_number")]
    [MaxLength(50)]
    public string? PhoneNumber { get; set; }

    [Column("date_of_birth")]
    public DateTime? DateOfBirth { get; set; }

    [Column("gender")]
    [MaxLength(20)]
    public string? Gender { get; set; }

    [Column("avatar_url")]
    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    [Column("status")]
    [MaxLength(50)]
    public string Status { get; set; } = "active";

    [Column("email_verified")]
    public bool EmailVerified { get; set; }

    [Column("phone_verified")]
    public bool PhoneVerified { get; set; }

    [Column("accepts_marketing")]
    public bool AcceptsMarketing { get; set; }

    [Column("marketing_opt_in_date")]
    public DateTime? MarketingOptInDate { get; set; }

    [Column("total_orders")]
    public int TotalOrders { get; set; }

    [Column("total_spent")]
    public decimal TotalSpent { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("tags")]
    [MaxLength(500)]
    public string? Tags { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<CustomerAddress> Addresses { get; set; } = new List<CustomerAddress>();
    public virtual ICollection<CustomerNote> Notes_Collection { get; set; } = new List<CustomerNote>();
    public virtual ICollection<CustomerSegmentMember> SegmentMemberships { get; set; } = new List<CustomerSegmentMember>();
}

[Table("customer_address", Schema = "fabrica")]
public class CustomerAddress
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("customer_id")]
    public Guid CustomerId { get; set; }

    [Required]
    [Column("address_type")]
    [MaxLength(50)]
    public string AddressType { get; set; } = "shipping";

    [Column("is_default")]
    public bool IsDefault { get; set; }

    [Column("first_name")]
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [Column("last_name")]
    [MaxLength(100)]
    public string? LastName { get; set; }

    [Column("company")]
    [MaxLength(255)]
    public string? Company { get; set; }

    [Required]
    [Column("address_line1")]
    [MaxLength(255)]
    public string AddressLine1 { get; set; } = string.Empty;

    [Column("address_line2")]
    [MaxLength(255)]
    public string? AddressLine2 { get; set; }

    [Required]
    [Column("city")]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Column("state_province")]
    [MaxLength(100)]
    public string? StateProvince { get; set; }

    [Column("postal_code")]
    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [Required]
    [Column("country")]
    [MaxLength(100)]
    public string Country { get; set; } = string.Empty;

    [Column("phone_number")]
    [MaxLength(50)]
    public string? PhoneNumber { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }
}

[Table("customer_note", Schema = "fabrica")]
public class CustomerNote
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("customer_id")]
    public Guid CustomerId { get; set; }

    [Column("note_type")]
    [MaxLength(50)]
    public string NoteType { get; set; } = "general";

    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }
}

[Table("customer_segment", Schema = "fabrica")]
public class CustomerSegment
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("tenant_id")]
    [MaxLength(100)]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column("slug")]
    [MaxLength(100)]
    public string Slug { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("criteria", TypeName = "jsonb")]
    public string? Criteria { get; set; }

    [Column("is_dynamic")]
    public bool IsDynamic { get; set; }

    [Column("customer_count")]
    public int CustomerCount { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<CustomerSegmentMember> Members { get; set; } = new List<CustomerSegmentMember>();
}

[Table("customer_segment_member", Schema = "fabrica")]
public class CustomerSegmentMember
{
    [Column("customer_id")]
    public Guid CustomerId { get; set; }

    [Column("segment_id")]
    public Guid SegmentId { get; set; }

    [Column("added_at")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    [ForeignKey("SegmentId")]
    public virtual CustomerSegment? Segment { get; set; }
}
