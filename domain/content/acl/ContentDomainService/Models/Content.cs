using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Fabrica.Domain.Esb.Interfaces;

namespace ContentDomainService.Models;

[Table("content", Schema = "fabrica")]
public class Content : IOutboxEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("tenant_id")]
    [MaxLength(100)]
    public string TenantId { get; set; } = string.Empty;

    [Column("content_type_id")]
    public Guid ContentTypeId { get; set; }

    [Column("parent_id")]
    public Guid? ParentId { get; set; }

    [Required]
    [Column("slug")]
    [MaxLength(500)]
    public string Slug { get; set; } = string.Empty;

    [Column("author_id")]
    public Guid? AuthorId { get; set; }

    [Column("featured_image_id")]
    public Guid? FeaturedImageId { get; set; }

    [Column("status")]
    [MaxLength(50)]
    public string Status { get; set; } = "draft";

    [Column("visibility")]
    [MaxLength(50)]
    public string Visibility { get; set; } = "public";

    [Column("password_hash")]
    [MaxLength(255)]
    public string? PasswordHash { get; set; }

    [Column("publish_at")]
    public DateTime? PublishAt { get; set; }

    [Column("unpublish_at")]
    public DateTime? UnpublishAt { get; set; }

    [Column("published_at")]
    public DateTime? PublishedAt { get; set; }

    [Column("view_count")]
    public int ViewCount { get; set; }

    [Column("is_featured")]
    public bool IsFeatured { get; set; }

    [Column("is_pinned")]
    public bool IsPinned { get; set; }

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("custom_data", TypeName = "jsonb")]
    public string? CustomData { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ContentType? ContentType { get; set; }
    public Content? Parent { get; set; }
    public ICollection<Content> Children { get; set; } = new List<Content>();
    public ICollection<ContentTranslation> Translations { get; set; } = new List<ContentTranslation>();
    public ICollection<ContentCategoryMapping> CategoryMappings { get; set; } = new List<ContentCategoryMapping>();
    public ICollection<ContentTagMapping> TagMappings { get; set; } = new List<ContentTagMapping>();
    public Media? FeaturedImage { get; set; }

    // IOutboxEntity implementation
    public string GetAggregateType() => "content";
    public Guid GetAggregateId() => Id;
    public string GetTenantId() => TenantId;
}
