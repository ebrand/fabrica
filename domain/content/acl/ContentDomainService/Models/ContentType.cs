using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContentDomainService.Models;

[Table("content_type", Schema = "fabrica")]
public class ContentType
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("tenant_id")]
    [MaxLength(100)]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    [Column("code")]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("icon")]
    [MaxLength(50)]
    public string? Icon { get; set; }

    [Column("has_slug")]
    public bool HasSlug { get; set; } = true;

    [Column("has_featured_image")]
    public bool HasFeaturedImage { get; set; } = true;

    [Column("has_excerpt")]
    public bool HasExcerpt { get; set; } = true;

    [Column("has_body")]
    public bool HasBody { get; set; } = true;

    [Column("has_seo")]
    public bool HasSeo { get; set; } = true;

    [Column("has_categories")]
    public bool HasCategories { get; set; } = true;

    [Column("has_tags")]
    public bool HasTags { get; set; } = true;

    [Column("has_author")]
    public bool HasAuthor { get; set; }

    [Column("has_publish_date")]
    public bool HasPublishDate { get; set; }

    [Column("is_hierarchical")]
    public bool IsHierarchical { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("custom_fields", TypeName = "jsonb")]
    public string? CustomFields { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Content> Contents { get; set; } = new List<Content>();
}
