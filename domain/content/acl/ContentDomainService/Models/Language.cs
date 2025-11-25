using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContentDomainService.Models;

[Table("language", Schema = "fabrica")]
public class Language
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("tenant_id")]
    [MaxLength(100)]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    [Column("locale_code")]
    [MaxLength(10)]
    public string LocaleCode { get; set; } = string.Empty;

    [Required]
    [Column("language_code")]
    [MaxLength(5)]
    public string LanguageCode { get; set; } = string.Empty;

    [Required]
    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("native_name")]
    [MaxLength(100)]
    public string? NativeName { get; set; }

    [Column("is_default")]
    public bool IsDefault { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("direction")]
    [MaxLength(3)]
    public string Direction { get; set; } = "ltr";

    [Column("date_format")]
    [MaxLength(50)]
    public string? DateFormat { get; set; }

    [Column("currency_code")]
    [MaxLength(3)]
    public string? CurrencyCode { get; set; }

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
