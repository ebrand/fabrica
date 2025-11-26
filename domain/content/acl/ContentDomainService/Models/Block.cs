using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContentDomainService.Models;

/// <summary>
/// Block template definition - defines the structure of a content block (Article, Card, etc.)
/// </summary>
[Table("block", Schema = "fabrica")]
public class Block
{
    [Key]
    [Column("block_id")]
    public Guid BlockId { get; set; }

    [Column("tenant_id")]
    [Required]
    [StringLength(100)]
    public string TenantId { get; set; } = string.Empty;

    [Column("name")]
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("slug")]
    [Required]
    [StringLength(100)]
    public string Slug { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("icon")]
    [StringLength(50)]
    public string? Icon { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<BlockSection> BlockSections { get; set; } = new List<BlockSection>();
    public ICollection<Variant> Variants { get; set; } = new List<Variant>();
    public ICollection<BlockContent> BlockContents { get; set; } = new List<BlockContent>();
}

/// <summary>
/// Section type definition - defines types of fields that can appear in blocks (Title, Subtitle, Body, etc.)
/// </summary>
[Table("section_type", Schema = "fabrica")]
public class SectionType
{
    [Key]
    [Column("section_type_id")]
    public Guid SectionTypeId { get; set; }

    [Column("tenant_id")]
    [Required]
    [StringLength(100)]
    public string TenantId { get; set; } = string.Empty;

    [Column("name")]
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("slug")]
    [Required]
    [StringLength(100)]
    public string Slug { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("field_type")]
    [StringLength(50)]
    public string FieldType { get; set; } = "text";

    [Column("validation_rules", TypeName = "jsonb")]
    public string? ValidationRules { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<BlockSection> BlockSections { get; set; } = new List<BlockSection>();
    public ICollection<BlockContentSectionTranslation> Translations { get; set; } = new List<BlockContentSectionTranslation>();
}

/// <summary>
/// Junction table linking blocks to their allowed section types
/// </summary>
[Table("block_section", Schema = "fabrica")]
public class BlockSection
{
    [Column("block_id")]
    public Guid BlockId { get; set; }

    [Column("section_type_id")]
    public Guid SectionTypeId { get; set; }

    [Column("is_required")]
    public bool IsRequired { get; set; }

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("default_value")]
    public string? DefaultValue { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("BlockId")]
    public Block? Block { get; set; }

    [ForeignKey("SectionTypeId")]
    public SectionType? SectionType { get; set; }
}

/// <summary>
/// Visual presentation styles for each block type
/// </summary>
[Table("variant", Schema = "fabrica")]
public class Variant
{
    [Key]
    [Column("variant_id")]
    public Guid VariantId { get; set; }

    [Column("block_id")]
    public Guid BlockId { get; set; }

    [Column("name")]
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("slug")]
    [Required]
    [StringLength(100)]
    public string Slug { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("preview_image_url")]
    [StringLength(500)]
    public string? PreviewImageUrl { get; set; }

    [Column("css_class")]
    [StringLength(100)]
    public string? CssClass { get; set; }

    [Column("settings", TypeName = "jsonb")]
    public string? Settings { get; set; }

    [Column("is_default")]
    public bool IsDefault { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("BlockId")]
    public Block? Block { get; set; }

    public ICollection<BlockContent> BlockContents { get; set; } = new List<BlockContent>();
}

/// <summary>
/// Content instances - actual content pieces created from block templates
/// </summary>
[Table("block_content", Schema = "fabrica")]
public class BlockContent
{
    [Key]
    [Column("content_id")]
    public Guid ContentId { get; set; }

    [Column("tenant_id")]
    [Required]
    [StringLength(100)]
    public string TenantId { get; set; } = string.Empty;

    [Column("block_id")]
    public Guid BlockId { get; set; }

    [Column("default_variant_id")]
    public Guid? DefaultVariantId { get; set; }

    [Column("slug")]
    [Required]
    [StringLength(255)]
    public string Slug { get; set; } = string.Empty;

    [Column("name")]
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("access_control")]
    [StringLength(50)]
    public string AccessControl { get; set; } = "everyone";

    [Column("visibility")]
    [StringLength(50)]
    public string Visibility { get; set; } = "public";

    [Column("publish_at")]
    public DateTime? PublishAt { get; set; }

    [Column("unpublish_at")]
    public DateTime? UnpublishAt { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("BlockId")]
    public Block? Block { get; set; }

    [ForeignKey("DefaultVariantId")]
    public Variant? DefaultVariant { get; set; }

    public ICollection<BlockContentSectionTranslation> SectionTranslations { get; set; } = new List<BlockContentSectionTranslation>();
    public ICollection<BlockContentCategory> CategoryMappings { get; set; } = new List<BlockContentCategory>();
    public ICollection<BlockContentTag> TagMappings { get; set; } = new List<BlockContentTag>();
}

/// <summary>
/// Translated content values for each section of each content piece
/// </summary>
[Table("block_content_section_translation", Schema = "fabrica")]
public class BlockContentSectionTranslation
{
    [Key]
    [Column("section_id")]
    public Guid SectionId { get; set; }

    [Column("content_id")]
    public Guid ContentId { get; set; }

    [Column("section_type_id")]
    public Guid SectionTypeId { get; set; }

    [Column("language_id")]
    public Guid LanguageId { get; set; }

    [Column("content")]
    public string? Content { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("ContentId")]
    public BlockContent? BlockContent { get; set; }

    [ForeignKey("SectionTypeId")]
    public SectionType? SectionType { get; set; }

    [ForeignKey("LanguageId")]
    public Language? Language { get; set; }
}

/// <summary>
/// Categories for organizing block content
/// </summary>
[Table("block_category", Schema = "fabrica")]
public class BlockCategory
{
    [Key]
    [Column("category_id")]
    public Guid CategoryId { get; set; }

    [Column("tenant_id")]
    [Required]
    [StringLength(100)]
    public string TenantId { get; set; } = string.Empty;

    [Column("parent_id")]
    public Guid? ParentId { get; set; }

    [Column("slug")]
    [Required]
    [StringLength(100)]
    public string Slug { get; set; } = string.Empty;

    [Column("icon")]
    [StringLength(50)]
    public string? Icon { get; set; }

    [Column("color")]
    [StringLength(20)]
    public string? Color { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("ParentId")]
    public BlockCategory? Parent { get; set; }

    public ICollection<BlockCategory> Children { get; set; } = new List<BlockCategory>();
    public ICollection<BlockCategoryTranslation> Translations { get; set; } = new List<BlockCategoryTranslation>();
    public ICollection<BlockContentCategory> ContentMappings { get; set; } = new List<BlockContentCategory>();
}

/// <summary>
/// Translated names and descriptions for categories
/// </summary>
[Table("block_category_translation", Schema = "fabrica")]
public class BlockCategoryTranslation
{
    [Key]
    [Column("translation_id")]
    public Guid TranslationId { get; set; }

    [Column("category_id")]
    public Guid CategoryId { get; set; }

    [Column("language_id")]
    public Guid LanguageId { get; set; }

    [Column("name")]
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("CategoryId")]
    public BlockCategory? Category { get; set; }

    [ForeignKey("LanguageId")]
    public Language? Language { get; set; }
}

/// <summary>
/// Junction table linking block content to categories
/// </summary>
[Table("block_content_category", Schema = "fabrica")]
public class BlockContentCategory
{
    [Column("content_id")]
    public Guid ContentId { get; set; }

    [Column("category_id")]
    public Guid CategoryId { get; set; }

    [Column("is_primary")]
    public bool IsPrimary { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("ContentId")]
    public BlockContent? Content { get; set; }

    [ForeignKey("CategoryId")]
    public BlockCategory? Category { get; set; }
}

/// <summary>
/// Tags for labeling block content
/// </summary>
[Table("block_tag", Schema = "fabrica")]
public class BlockTag
{
    [Key]
    [Column("tag_id")]
    public Guid TagId { get; set; }

    [Column("tenant_id")]
    [Required]
    [StringLength(100)]
    public string TenantId { get; set; } = string.Empty;

    [Column("slug")]
    [Required]
    [StringLength(100)]
    public string Slug { get; set; } = string.Empty;

    [Column("color")]
    [StringLength(20)]
    public string? Color { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<BlockTagTranslation> Translations { get; set; } = new List<BlockTagTranslation>();
    public ICollection<BlockContentTag> ContentMappings { get; set; } = new List<BlockContentTag>();
}

/// <summary>
/// Translated names for tags
/// </summary>
[Table("block_tag_translation", Schema = "fabrica")]
public class BlockTagTranslation
{
    [Key]
    [Column("translation_id")]
    public Guid TranslationId { get; set; }

    [Column("tag_id")]
    public Guid TagId { get; set; }

    [Column("language_id")]
    public Guid LanguageId { get; set; }

    [Column("name")]
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("TagId")]
    public BlockTag? Tag { get; set; }

    [ForeignKey("LanguageId")]
    public Language? Language { get; set; }
}

/// <summary>
/// Junction table linking block content to tags
/// </summary>
[Table("block_content_tag", Schema = "fabrica")]
public class BlockContentTag
{
    [Column("content_id")]
    public Guid ContentId { get; set; }

    [Column("tag_id")]
    public Guid TagId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("ContentId")]
    public BlockContent? Content { get; set; }

    [ForeignKey("TagId")]
    public BlockTag? Tag { get; set; }
}
