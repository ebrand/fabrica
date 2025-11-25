using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Fabrica.Domain.Esb.Interfaces;

namespace ContentDomainService.Models;

[Table("media_folder", Schema = "fabrica")]
public class MediaFolder
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
    [Column("name")]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column("slug")]
    [MaxLength(255)]
    public string Slug { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public MediaFolder? Parent { get; set; }
    public ICollection<MediaFolder> Children { get; set; } = new List<MediaFolder>();
    public ICollection<Media> Media { get; set; } = new List<Media>();
}

[Table("media", Schema = "fabrica")]
public class Media : IOutboxEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("tenant_id")]
    [MaxLength(100)]
    public string TenantId { get; set; } = string.Empty;

    [Column("folder_id")]
    public Guid? FolderId { get; set; }

    [Required]
    [Column("file_name")]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [Column("original_file_name")]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [Column("file_path")]
    [MaxLength(1000)]
    public string FilePath { get; set; } = string.Empty;

    [Required]
    [Column("file_url")]
    [MaxLength(1000)]
    public string FileUrl { get; set; } = string.Empty;

    [Required]
    [Column("mime_type")]
    [MaxLength(100)]
    public string MimeType { get; set; } = string.Empty;

    [Column("file_size")]
    public long FileSize { get; set; }

    [Column("file_extension")]
    [MaxLength(20)]
    public string? FileExtension { get; set; }

    [Required]
    [Column("media_type")]
    [MaxLength(50)]
    public string MediaType { get; set; } = string.Empty;

    [Column("width")]
    public int? Width { get; set; }

    [Column("height")]
    public int? Height { get; set; }

    [Column("duration")]
    public int? Duration { get; set; }

    [Column("thumbnail_url")]
    [MaxLength(1000)]
    public string? ThumbnailUrl { get; set; }

    [Column("blurhash")]
    [MaxLength(100)]
    public string? Blurhash { get; set; }

    [Column("metadata", TypeName = "jsonb")]
    public string? Metadata { get; set; }

    [Column("is_public")]
    public bool IsPublic { get; set; } = true;

    [Column("uploaded_by")]
    public Guid? UploadedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public MediaFolder? Folder { get; set; }
    public ICollection<MediaTranslation> Translations { get; set; } = new List<MediaTranslation>();

    // IOutboxEntity implementation
    public string GetAggregateType() => "media";
    public Guid GetAggregateId() => Id;
    public string GetTenantId() => TenantId;
}

[Table("media_translation", Schema = "fabrica")]
public class MediaTranslation
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("media_id")]
    public Guid MediaId { get; set; }

    [Required]
    [Column("locale_code")]
    [MaxLength(10)]
    public string LocaleCode { get; set; } = string.Empty;

    [Column("alt_text")]
    [MaxLength(500)]
    public string? AltText { get; set; }

    [Column("title")]
    [MaxLength(255)]
    public string? Title { get; set; }

    [Column("caption")]
    public string? Caption { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Media? Media { get; set; }
}
