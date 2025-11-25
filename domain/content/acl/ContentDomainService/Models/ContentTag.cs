using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContentDomainService.Models;

[Table("content_tag", Schema = "fabrica")]
public class ContentTag
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("tenant_id")]
    [MaxLength(100)]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    [Column("slug")]
    [MaxLength(100)]
    public string Slug { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<ContentTagTranslation> Translations { get; set; } = new List<ContentTagTranslation>();
    public ICollection<ContentTagMapping> ContentMappings { get; set; } = new List<ContentTagMapping>();
}

[Table("content_tag_translation", Schema = "fabrica")]
public class ContentTagTranslation
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("content_tag_id")]
    public Guid ContentTagId { get; set; }

    [Required]
    [Column("locale_code")]
    [MaxLength(10)]
    public string LocaleCode { get; set; } = string.Empty;

    [Required]
    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ContentTag? ContentTag { get; set; }
}

[Table("content_tag_mapping", Schema = "fabrica")]
public class ContentTagMapping
{
    [Column("content_id")]
    public Guid ContentId { get; set; }

    [Column("content_tag_id")]
    public Guid ContentTagId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Content? Content { get; set; }
    public ContentTag? ContentTag { get; set; }
}
