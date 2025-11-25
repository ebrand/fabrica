using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fabrica.Domain.Esb.Models;

/// <summary>
/// Stores cached data from events consumed from other domains.
/// Acts as a local read-model/cache for cross-domain data.
/// </summary>
[Table("cache", Schema = "cache")]
public class CacheEntry
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
    /// The source table/aggregate type (e.g., "product", "user")
    /// </summary>
    [Required]
    [Column("source_table")]
    [MaxLength(100)]
    public string SourceTable { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the entity in the source system
    /// </summary>
    [Required]
    [Column("aggregate_id")]
    public Guid AggregateId { get; set; }

    /// <summary>
    /// The tenant ID from the source event
    /// </summary>
    [Required]
    [Column("tenant_id")]
    [MaxLength(100)]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// The last event type that modified this cache entry (e.g., "product.created", "product.updated")
    /// </summary>
    [Required]
    [Column("last_event_type")]
    [MaxLength(100)]
    public string LastEventType { get; set; } = string.Empty;

    /// <summary>
    /// The cached data as JSON
    /// </summary>
    [Required]
    [Column("cache_data", TypeName = "jsonb")]
    public string CacheData { get; set; } = "{}";

    /// <summary>
    /// Version number for optimistic concurrency and ordering
    /// Increments with each update
    /// </summary>
    [Column("version")]
    public long Version { get; set; } = 1;

    /// <summary>
    /// Whether this entry is marked as deleted (soft delete)
    /// Set to true when a delete event is received
    /// </summary>
    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// When this entry was first cached
    /// </summary>
    [Column("cached_at")]
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this entry was last updated
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this cache entry expires (null = never expires)
    /// </summary>
    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// The original event ID from the outbox that created/updated this entry
    /// Useful for idempotency and debugging
    /// </summary>
    [Column("source_event_id")]
    public Guid? SourceEventId { get; set; }

    /// <summary>
    /// Timestamp of the original event in the source domain
    /// </summary>
    [Column("source_event_time")]
    public DateTime? SourceEventTime { get; set; }
}
