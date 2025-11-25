using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fabrica.Domain.Esb.Models;

/// <summary>
/// Configuration table that controls which events from other domains to consume and cache.
/// Mirrors the cdc.outbox_config pattern but for the consumption side.
/// </summary>
[Table("cache_config", Schema = "cache")]
public class CacheConfig
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The source domain name (e.g., "product", "admin", "order")
    /// </summary>
    [Required]
    [Column("source_domain")]
    [MaxLength(100)]
    public string SourceDomain { get; set; } = string.Empty;

    /// <summary>
    /// The source schema name (e.g., "fabrica")
    /// </summary>
    [Required]
    [Column("source_schema")]
    [MaxLength(100)]
    public string SourceSchema { get; set; } = string.Empty;

    /// <summary>
    /// The source table name (e.g., "product", "user")
    /// </summary>
    [Required]
    [Column("source_table")]
    [MaxLength(100)]
    public string SourceTable { get; set; } = string.Empty;

    /// <summary>
    /// Whether to listen for and cache CREATE events (e.g., "product.created")
    /// </summary>
    [Column("listen_create")]
    public bool ListenCreate { get; set; } = true;

    /// <summary>
    /// Whether to listen for and cache UPDATE events (e.g., "product.updated")
    /// </summary>
    [Column("listen_update")]
    public bool ListenUpdate { get; set; } = true;

    /// <summary>
    /// Whether to listen for and cache DELETE events (e.g., "product.deleted")
    /// </summary>
    [Column("listen_delete")]
    public bool ListenDelete { get; set; } = true;

    /// <summary>
    /// Whether this configuration is active
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional TTL in seconds for cached entries (null = no expiration)
    /// </summary>
    [Column("cache_ttl_seconds")]
    public int? CacheTtlSeconds { get; set; }

    /// <summary>
    /// Optional description for this configuration
    /// </summary>
    [Column("description")]
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// The Kafka consumer group name for this subscription
    /// </summary>
    [Required]
    [Column("consumer_group")]
    [MaxLength(255)]
    public string ConsumerGroup { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the Kafka topic name for created events
    /// </summary>
    [NotMapped]
    public string CreatedTopic => $"{SourceTable}.created";

    /// <summary>
    /// Gets the Kafka topic name for updated events
    /// </summary>
    [NotMapped]
    public string UpdatedTopic => $"{SourceTable}.updated";

    /// <summary>
    /// Gets the Kafka topic name for deleted events
    /// </summary>
    [NotMapped]
    public string DeletedTopic => $"{SourceTable}.deleted";
}
