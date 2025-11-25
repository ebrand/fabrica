using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContentDomainService.Models;

[Table("content_category", Schema = "fabrica")]
public class ContentCategory
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("tenant_id")]
    [MaxLength(100)]
    public string TenantId { get; set; } = string.Empty;

    [Column("parent_id")]
    public Guid? ParentId { get; set; }

    [Required]
    [Column("slug")]
    [MaxLength(255)]
    public string Slug { get; set; } = string.Empty;

    [Column("image_url")]
    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ContentCategory? Parent { get; set; }
    public ICollection<ContentCategory> Children { get; set; } = new List<ContentCategory>();
    public ICollection<ContentCategoryTranslation> Translations { get; set; } = new List<ContentCategoryTranslation>();
    public ICollection<ContentCategoryMapping> ContentMappings { get; set; } = new List<ContentCategoryMapping>();
}

[Table("content_category_translation", Schema = "fabrica")]
public class ContentCategoryTranslation
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("content_category_id")]
    public Guid ContentCategoryId { get; set; }

    [Required]
    [Column("locale_code")]
    [MaxLength(10)]
    public string LocaleCode { get; set; } = string.Empty;

    [Required]
    [Column("name")]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("seo_title")]
    [MaxLength(255)]
    public string? SeoTitle { get; set; }

    [Column("seo_description")]
    public string? SeoDescription { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ContentCategory? ContentCategory { get; set; }
}

[Table("content_category_mapping", Schema = "fabrica")]
public class ContentCategoryMapping
{
    [Column("content_id")]
    public Guid ContentId { get; set; }

    [Column("content_category_id")]
    public Guid ContentCategoryId { get; set; }

    [Column("is_primary")]
    public bool IsPrimary { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Content? Content { get; set; }
    public ContentCategory? ContentCategory { get; set; }
}
