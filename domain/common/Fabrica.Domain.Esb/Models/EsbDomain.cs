using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fabrica.Domain.Esb.Models;

/// <summary>
/// Registry of all domains participating in the Enterprise Service Bus (ESB).
/// This table is managed in the admin database and tracks all domains that can
/// publish or consume events.
/// </summary>
[Table("domain", Schema = "fabrica")]
public class EsbDomain
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Unique identifier for the domain (e.g., "product", "admin", "order")
    /// </summary>
    [Required]
    [Column("domain_name")]
    [MaxLength(100)]
    public string DomainName { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name (e.g., "Product Domain")
    /// </summary>
    [Required]
    [Column("display_name")]
    [MaxLength(255)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description of the domain's responsibilities
    /// </summary>
    [Column("description")]
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Base URL of the domain service (e.g., "http://product-service:3420")
    /// </summary>
    [Column("service_url")]
    [MaxLength(500)]
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// Prefix for Kafka topics (usually same as domain_name)
    /// Events will be published as {prefix}.{event_type}
    /// </summary>
    [Column("kafka_topic_prefix")]
    [MaxLength(100)]
    public string? KafkaTopicPrefix { get; set; }

    /// <summary>
    /// The database schema used by this domain (default: "fabrica")
    /// </summary>
    [Required]
    [Column("schema_name")]
    [MaxLength(100)]
    public string SchemaName { get; set; } = "fabrica";

    /// <summary>
    /// The database name used by this domain (e.g., "fabrica-product-db")
    /// </summary>
    [Column("database_name")]
    [MaxLength(100)]
    public string? DatabaseName { get; set; }

    /// <summary>
    /// Whether this domain publishes events to the ESB
    /// </summary>
    [Column("publishes_events")]
    public bool PublishesEvents { get; set; } = true;

    /// <summary>
    /// Whether this domain consumes events from the ESB
    /// </summary>
    [Column("consumes_events")]
    public bool ConsumesEvents { get; set; } = true;

    /// <summary>
    /// Whether this domain is currently active
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this domain has a shell application
    /// </summary>
    [Column("has_shell")]
    public bool HasShell { get; set; } = false;

    /// <summary>
    /// Whether this domain has a micro-frontend
    /// </summary>
    [Column("has_mfe")]
    public bool HasMfe { get; set; } = false;

    /// <summary>
    /// Whether this domain has a backend-for-frontend service
    /// </summary>
    [Column("has_bff")]
    public bool HasBff { get; set; } = false;

    /// <summary>
    /// Whether this domain has an anti-corruption layer service
    /// </summary>
    [Column("has_acl")]
    public bool HasAcl { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
