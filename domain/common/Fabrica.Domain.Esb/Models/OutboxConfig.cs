using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fabrica.Domain.Esb.Models;

/// <summary>
/// Configuration table that controls which tables and actions write events to the outbox.
/// </summary>
[Table("outbox_config", Schema = "cdc")]
public class OutboxConfig
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The schema name of the table (e.g., "fabrica")
    /// </summary>
    [Required]
    [Column("schema_name")]
    [MaxLength(100)]
    public string SchemaName { get; set; } = string.Empty;

    /// <summary>
    /// The table name to track (e.g., "product", "user")
    /// </summary>
    [Required]
    [Column("table_name")]
    [MaxLength(100)]
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Whether to capture INSERT/Create events
    /// </summary>
    [Column("capture_insert")]
    public bool CaptureInsert { get; set; } = true;

    /// <summary>
    /// Whether to capture UPDATE/Modify events
    /// </summary>
    [Column("capture_update")]
    public bool CaptureUpdate { get; set; } = true;

    /// <summary>
    /// Whether to capture DELETE events
    /// </summary>
    [Column("capture_delete")]
    public bool CaptureDelete { get; set; } = true;

    /// <summary>
    /// Whether this configuration is active
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional description for this configuration
    /// </summary>
    [Column("description")]
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// The domain name (e.g., "admin", "product")
    /// </summary>
    [Required]
    [Column("domain_name")]
    [MaxLength(100)]
    public string DomainName { get; set; } = string.Empty;

    /// <summary>
    /// The Kafka topic name for publishing CDC events
    /// </summary>
    [Required]
    [Column("topic_name")]
    [MaxLength(255)]
    public string TopicName { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
