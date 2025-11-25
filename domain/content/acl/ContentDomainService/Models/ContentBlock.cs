using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContentDomainService.Models;

[Table("content_block", Schema = "fabrica")]
public class ContentBlock
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("tenant_id")]
    [Required]
    [StringLength(100)]
    public string TenantId { get; set; } = string.Empty;

    [Column("code")]
    [Required]
    [StringLength(100)]
    public string Code { get; set; } = string.Empty;

    [Column("block_type")]
    [Required]
    [StringLength(50)]
    public string BlockType { get; set; } = string.Empty;

    [Column("settings", TypeName = "jsonb")]
    public string? Settings { get; set; }

    [Column("is_global")]
    public bool IsGlobal { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<ContentBlockTranslation> Translations { get; set; } = new List<ContentBlockTranslation>();
}

[Table("content_block_translation", Schema = "fabrica")]
public class ContentBlockTranslation
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("content_block_id")]
    public Guid ContentBlockId { get; set; }

    [Column("locale_code")]
    [Required]
    [StringLength(10)]
    public string LocaleCode { get; set; } = string.Empty;

    [Column("title")]
    [StringLength(255)]
    public string? Title { get; set; }

    [Column("subtitle")]
    [StringLength(500)]
    public string? Subtitle { get; set; }

    [Column("body")]
    public string? Body { get; set; }

    [Column("cta_text")]
    [StringLength(100)]
    public string? CtaText { get; set; }

    [Column("cta_url")]
    [StringLength(500)]
    public string? CtaUrl { get; set; }

    [Column("additional_data", TypeName = "jsonb")]
    public string? AdditionalData { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("ContentBlockId")]
    public ContentBlock? ContentBlock { get; set; }
}
