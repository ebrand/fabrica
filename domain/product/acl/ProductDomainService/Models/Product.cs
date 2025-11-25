using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Fabrica.Domain.Esb.Interfaces;

namespace ProductDomainService.Models;

[Table("product", Schema = "fabrica")]
public class Product : IOutboxEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("tenant_id")]
    [MaxLength(100)]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    [Column("sku")]
    [MaxLength(100)]
    public string Sku { get; set; } = string.Empty;

    [Required]
    [Column("name")]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column("slug")]
    [MaxLength(255)]
    public string Slug { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("short_description")]
    [MaxLength(500)]
    public string? ShortDescription { get; set; }

    [Required]
    [Column("base_price")]
    public decimal BasePrice { get; set; }

    [Column("compare_at_price")]
    public decimal? CompareAtPrice { get; set; }

    [Column("cost_price")]
    public decimal? CostPrice { get; set; }

    [Column("primary_image_url")]
    [MaxLength(500)]
    public string? PrimaryImageUrl { get; set; }

    [Column("status")]
    [MaxLength(50)]
    public string Status { get; set; } = "draft";

    [Column("product_type")]
    [MaxLength(100)]
    public string? ProductType { get; set; }

    [Column("vendor")]
    [MaxLength(255)]
    public string? Vendor { get; set; }

    [Column("weight")]
    public decimal? Weight { get; set; }

    [Column("weight_unit")]
    [MaxLength(20)]
    public string WeightUnit { get; set; } = "lb";

    [Column("requires_shipping")]
    public bool RequiresShipping { get; set; } = true;

    [Column("is_taxable")]
    public bool IsTaxable { get; set; } = true;

    [Column("track_inventory")]
    public bool TrackInventory { get; set; } = true;

    [Column("seo_meta_title")]
    [MaxLength(255)]
    public string? SeoMetaTitle { get; set; }

    [Column("seo_meta_description")]
    public string? SeoMetaDescription { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public virtual ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public virtual ICollection<ProductTag> Tags { get; set; } = new List<ProductTag>();
    public virtual ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();
}

[Table("product_image", Schema = "fabrica")]
public class ProductImage
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("product_id")]
    public Guid ProductId { get; set; }

    [Required]
    [Column("image_url")]
    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [Column("alt_text")]
    [MaxLength(255)]
    public string? AltText { get; set; }

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }
}

[Table("product_variant", Schema = "fabrica")]
public class ProductVariant
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("product_id")]
    public Guid ProductId { get; set; }

    [Required]
    [Column("sku")]
    [MaxLength(100)]
    public string Sku { get; set; } = string.Empty;

    [Column("name")]
    [MaxLength(255)]
    public string? Name { get; set; }

    [Column("price")]
    public decimal? Price { get; set; }

    [Column("compare_at_price")]
    public decimal? CompareAtPrice { get; set; }

    [Column("cost_price")]
    public decimal? CostPrice { get; set; }

    [Column("inventory_quantity")]
    public int InventoryQuantity { get; set; }

    [Column("image_url")]
    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    [Column("weight")]
    public decimal? Weight { get; set; }

    [Column("position")]
    public int Position { get; set; }

    [Column("option1_name")]
    [MaxLength(100)]
    public string? Option1Name { get; set; }

    [Column("option1_value")]
    [MaxLength(100)]
    public string? Option1Value { get; set; }

    [Column("option2_name")]
    [MaxLength(100)]
    public string? Option2Name { get; set; }

    [Column("option2_value")]
    [MaxLength(100)]
    public string? Option2Value { get; set; }

    [Column("option3_name")]
    [MaxLength(100)]
    public string? Option3Name { get; set; }

    [Column("option3_value")]
    [MaxLength(100)]
    public string? Option3Value { get; set; }

    [Column("barcode")]
    [MaxLength(100)]
    public string? Barcode { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }
}

[Table("product_tag", Schema = "fabrica")]
public class ProductTag
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("product_id")]
    public Guid ProductId { get; set; }

    [Required]
    [Column("tag")]
    [MaxLength(100)]
    public string Tag { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }
}
