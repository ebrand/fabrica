using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductDomainService.Models;

[Table("inventory", Schema = "fabrica")]
public class Inventory
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("tenant_id")]
    [MaxLength(100)]
    public string TenantId { get; set; } = string.Empty;

    [Column("product_id")]
    public Guid? ProductId { get; set; }

    [Column("product_variant_id")]
    public Guid? ProductVariantId { get; set; }

    [Required]
    [Column("location_id")]
    [MaxLength(100)]
    public string LocationId { get; set; } = string.Empty;

    [Column("quantity_available")]
    public int QuantityAvailable { get; set; } = 0;

    [Column("quantity_reserved")]
    public int QuantityReserved { get; set; } = 0;

    [Column("quantity_incoming")]
    public int? QuantityIncoming { get; set; }

    [Column("reorder_point")]
    public int? ReorderPoint { get; set; }

    [Column("reorder_quantity")]
    public int? ReorderQuantity { get; set; }

    [Column("last_restock_date")]
    public DateTime? LastRestockDate { get; set; }

    [Column("next_restock_date")]
    public DateTime? NextRestockDate { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }

    [ForeignKey("ProductVariantId")]
    public virtual ProductVariant? ProductVariant { get; set; }
}
