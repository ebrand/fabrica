using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContentDomainService.Models;

[Table("content_translation", Schema = "fabrica")]
public class ContentTranslation
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("content_id")]
    public Guid ContentId { get; set; }

    [Required]
    [Column("locale_code")]
    [MaxLength(10)]
    public string LocaleCode { get; set; } = string.Empty;

    [Required]
    [Column("title")]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [Column("excerpt")]
    public string? Excerpt { get; set; }

    [Column("body")]
    public string? Body { get; set; }

    [Column("seo_title")]
    [MaxLength(255)]
    public string? SeoTitle { get; set; }

    [Column("seo_description")]
    public string? SeoDescription { get; set; }

    [Column("seo_keywords")]
    public string? SeoKeywords { get; set; }

    [Column("og_title")]
    [MaxLength(255)]
    public string? OgTitle { get; set; }

    [Column("og_description")]
    public string? OgDescription { get; set; }

    [Column("og_image_url")]
    [MaxLength(500)]
    public string? OgImageUrl { get; set; }

    [Column("translation_status")]
    [MaxLength(50)]
    public string TranslationStatus { get; set; } = "draft";

    [Column("translator_id")]
    public Guid? TranslatorId { get; set; }

    [Column("reviewed_at")]
    public DateTime? ReviewedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Content? Content { get; set; }
}
