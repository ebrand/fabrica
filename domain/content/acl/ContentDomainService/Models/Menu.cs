using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContentDomainService.Models;

[Table("menu", Schema = "fabrica")]
public class Menu
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
    [MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Column("name")]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Column("location")]
    [MaxLength(100)]
    public string? Location { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<MenuItem> Items { get; set; } = new List<MenuItem>();
}

[Table("menu_item", Schema = "fabrica")]
public class MenuItem
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("menu_id")]
    public Guid MenuId { get; set; }

    [Column("parent_id")]
    public Guid? ParentId { get; set; }

    [Required]
    [Column("link_type")]
    [MaxLength(50)]
    public string LinkType { get; set; } = string.Empty;

    [Column("content_id")]
    public Guid? ContentId { get; set; }

    [Column("category_id")]
    public Guid? CategoryId { get; set; }

    [Column("url")]
    [MaxLength(1000)]
    public string? Url { get; set; }

    [Column("target")]
    [MaxLength(20)]
    public string Target { get; set; } = "_self";

    [Column("icon")]
    [MaxLength(50)]
    public string? Icon { get; set; }

    [Column("css_class")]
    [MaxLength(100)]
    public string? CssClass { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Menu? Menu { get; set; }
    public MenuItem? Parent { get; set; }
    public ICollection<MenuItem> Children { get; set; } = new List<MenuItem>();
    public Content? Content { get; set; }
    public ContentCategory? Category { get; set; }
    public ICollection<MenuItemTranslation> Translations { get; set; } = new List<MenuItemTranslation>();
}

[Table("menu_item_translation", Schema = "fabrica")]
public class MenuItemTranslation
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("menu_item_id")]
    public Guid MenuItemId { get; set; }

    [Required]
    [Column("locale_code")]
    [MaxLength(10)]
    public string LocaleCode { get; set; } = string.Empty;

    [Required]
    [Column("label")]
    [MaxLength(255)]
    public string Label { get; set; } = string.Empty;

    [Column("title")]
    [MaxLength(255)]
    public string? Title { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public MenuItem? MenuItem { get; set; }
}
