using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fabrica.Domain.Esb.Models;

/// <summary>
/// Represents an event in the outbox table for reliable event publishing.
/// Maps to the cdc.outbox table.
/// </summary>
[Table("outbox", Schema = "cdc")]
public class OutboxEvent
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("tenant_id")]
    [MaxLength(100)]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// The type of aggregate (e.g., "product", "user", "category")
    /// </summary>
    [Required]
    [Column("aggregate_type")]
    [MaxLength(100)]
    public string AggregateType { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the aggregate that was modified
    /// </summary>
    [Required]
    [Column("aggregate_id")]
    public Guid AggregateId { get; set; }

    /// <summary>
    /// The type of event (e.g., "product.created", "user.updated", "category.deleted")
    /// </summary>
    [Required]
    [Column("event_type")]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// JSON representation of the event data (the entity state)
    /// </summary>
    [Required]
    [Column("event_data", TypeName = "jsonb")]
    public string EventData { get; set; } = "{}";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Status of the event: pending, processing, processed, failed
    /// </summary>
    [Column("status")]
    [MaxLength(50)]
    public string Status { get; set; } = "pending";
}
