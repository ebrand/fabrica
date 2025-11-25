using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Fabrica.Domain.Esb.Interceptors;
using Fabrica.Domain.Esb.Models;

namespace Fabrica.Domain.Esb.Extensions;

/// <summary>
/// Extension methods for configuring the outbox pattern in EF Core DbContexts.
/// </summary>
public static class OutboxExtensions
{
    /// <summary>
    /// Adds the outbox SaveChanges interceptor to the DbContext options.
    /// Use this when configuring your DbContext in Program.cs.
    /// </summary>
    /// <example>
    /// services.AddDbContext&lt;MyDbContext&gt;(options =>
    ///     options.UseNpgsql(connectionString)
    ///            .AddOutboxInterceptor());
    /// </example>
    public static DbContextOptionsBuilder AddOutboxInterceptor(this DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(new OutboxSaveChangesInterceptor());
        return optionsBuilder;
    }

    /// <summary>
    /// Configures the OutboxEvent and OutboxConfig entity mappings in the DbContext OnModelCreating method.
    /// Call this from your DbContext's OnModelCreating method.
    /// </summary>
    /// <example>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     modelBuilder.ConfigureOutbox();
    ///     // ... other configurations
    /// }
    /// </example>
    public static ModelBuilder ConfigureOutbox(this ModelBuilder modelBuilder)
    {
        // Configure OutboxEvent entity
        modelBuilder.Entity<OutboxEvent>(entity =>
        {
            entity.ToTable("outbox", "cdc");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.AggregateType)
                .HasColumnName("aggregate_type")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.AggregateId)
                .HasColumnName("aggregate_id")
                .IsRequired();

            entity.Property(e => e.EventType)
                .HasColumnName("event_type")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.EventData)
                .HasColumnName("event_data")
                .HasColumnType("jsonb")
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.ProcessedAt)
                .HasColumnName("processed_at");

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(50)
                .HasDefaultValue("pending");

            // Indexes for performance
            entity.HasIndex(e => e.TenantId).HasDatabaseName("idx_outbox_tenant");
            entity.HasIndex(e => e.Status).HasDatabaseName("idx_outbox_status");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("idx_outbox_created");
        });

        // Configure OutboxConfig entity
        modelBuilder.Entity<OutboxConfig>(entity =>
        {
            entity.ToTable("outbox_config", "cdc");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.SchemaName)
                .HasColumnName("schema_name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.TableName)
                .HasColumnName("table_name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.CaptureInsert)
                .HasColumnName("capture_insert")
                .HasDefaultValue(true);

            entity.Property(e => e.CaptureUpdate)
                .HasColumnName("capture_update")
                .HasDefaultValue(true);

            entity.Property(e => e.CaptureDelete)
                .HasColumnName("capture_delete")
                .HasDefaultValue(true);

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            entity.Property(e => e.Description)
                .HasColumnName("description")
                .HasMaxLength(500);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Unique constraint on schema + table name
            entity.HasIndex(e => new { e.SchemaName, e.TableName })
                .IsUnique()
                .HasDatabaseName("idx_outbox_config_schema_table");

            // Index for active configs
            entity.HasIndex(e => e.IsActive).HasDatabaseName("idx_outbox_config_active");
        });

        return modelBuilder;
    }

    /// <summary>
    /// Registers the OutboxSaveChangesInterceptor as a singleton service.
    /// Alternative to AddOutboxInterceptor for more control over lifecycle.
    /// </summary>
    public static IServiceCollection AddOutboxServices(this IServiceCollection services)
    {
        services.AddSingleton<OutboxSaveChangesInterceptor>();
        return services;
    }

    /// <summary>
    /// Configures the CacheEntry and CacheConfig entity mappings in the DbContext OnModelCreating method.
    /// Use this for domains that consume events from other domains and cache the data locally.
    /// </summary>
    /// <example>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     modelBuilder.ConfigureCache();
    ///     // ... other configurations
    /// }
    /// </example>
    public static ModelBuilder ConfigureCache(this ModelBuilder modelBuilder)
    {
        // Configure CacheEntry entity
        modelBuilder.Entity<CacheEntry>(entity =>
        {
            entity.ToTable("cache", "cache");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.SourceDomain)
                .HasColumnName("source_domain")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.SourceTable)
                .HasColumnName("source_table")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.AggregateId)
                .HasColumnName("aggregate_id")
                .IsRequired();

            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.LastEventType)
                .HasColumnName("last_event_type")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.CacheData)
                .HasColumnName("cache_data")
                .HasColumnType("jsonb")
                .IsRequired();

            entity.Property(e => e.Version)
                .HasColumnName("version")
                .HasDefaultValue(1L);

            entity.Property(e => e.IsDeleted)
                .HasColumnName("is_deleted")
                .HasDefaultValue(false);

            entity.Property(e => e.CachedAt)
                .HasColumnName("cached_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.ExpiresAt)
                .HasColumnName("expires_at");

            entity.Property(e => e.SourceEventId)
                .HasColumnName("source_event_id");

            entity.Property(e => e.SourceEventTime)
                .HasColumnName("source_event_time");

            // Unique constraint on source_domain + source_table + aggregate_id
            // Each entity from a source can only have one cache entry
            entity.HasIndex(e => new { e.SourceDomain, e.SourceTable, e.AggregateId })
                .IsUnique()
                .HasDatabaseName("idx_cache_source_aggregate");

            // Index for tenant queries
            entity.HasIndex(e => e.TenantId).HasDatabaseName("idx_cache_tenant");

            // Index for finding non-deleted entries
            entity.HasIndex(e => e.IsDeleted).HasDatabaseName("idx_cache_deleted");

            // Index for TTL cleanup
            entity.HasIndex(e => e.ExpiresAt).HasDatabaseName("idx_cache_expires");

            // Composite index for common queries
            entity.HasIndex(e => new { e.SourceDomain, e.SourceTable, e.TenantId, e.IsDeleted })
                .HasDatabaseName("idx_cache_lookup");
        });

        // Configure CacheConfig entity
        modelBuilder.Entity<CacheConfig>(entity =>
        {
            entity.ToTable("cache_config", "cache");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.SourceDomain)
                .HasColumnName("source_domain")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.SourceSchema)
                .HasColumnName("source_schema")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.SourceTable)
                .HasColumnName("source_table")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.ListenCreate)
                .HasColumnName("listen_create")
                .HasDefaultValue(true);

            entity.Property(e => e.ListenUpdate)
                .HasColumnName("listen_update")
                .HasDefaultValue(true);

            entity.Property(e => e.ListenDelete)
                .HasColumnName("listen_delete")
                .HasDefaultValue(true);

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            entity.Property(e => e.CacheTtlSeconds)
                .HasColumnName("cache_ttl_seconds");

            entity.Property(e => e.Description)
                .HasColumnName("description")
                .HasMaxLength(500);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Unique constraint on source_domain + source_schema + source_table
            entity.HasIndex(e => new { e.SourceDomain, e.SourceSchema, e.SourceTable })
                .IsUnique()
                .HasDatabaseName("idx_cache_config_source");

            // Index for active configs
            entity.HasIndex(e => e.IsActive).HasDatabaseName("idx_cache_config_active");
        });

        return modelBuilder;
    }

    /// <summary>
    /// Configures the EsbDomain entity mapping in the DbContext OnModelCreating method.
    /// Use this for the admin domain which maintains the ESB domain registry.
    /// </summary>
    /// <example>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     modelBuilder.ConfigureEsbDomain();
    ///     // ... other configurations
    /// }
    /// </example>
    public static ModelBuilder ConfigureEsbDomain(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EsbDomain>(entity =>
        {
            entity.ToTable("domain", "fabrica");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("uuid_generate_v4()");

            entity.Property(e => e.DomainName)
                .HasColumnName("domain_name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.DisplayName)
                .HasColumnName("display_name")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasColumnName("description")
                .HasMaxLength(500);

            entity.Property(e => e.ServiceUrl)
                .HasColumnName("service_url")
                .HasMaxLength(500);

            entity.Property(e => e.KafkaTopicPrefix)
                .HasColumnName("kafka_topic_prefix")
                .HasMaxLength(100);

            entity.Property(e => e.SchemaName)
                .HasColumnName("schema_name")
                .HasMaxLength(100)
                .HasDefaultValue("fabrica")
                .IsRequired();

            entity.Property(e => e.DatabaseName)
                .HasColumnName("database_name")
                .HasMaxLength(100);

            entity.Property(e => e.PublishesEvents)
                .HasColumnName("publishes_events")
                .HasDefaultValue(true);

            entity.Property(e => e.ConsumesEvents)
                .HasColumnName("consumes_events")
                .HasDefaultValue(true);

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Unique constraint on domain_name
            entity.HasIndex(e => e.DomainName)
                .IsUnique()
                .HasDatabaseName("idx_domain_name");

            // Indexes for common queries
            entity.HasIndex(e => e.IsActive).HasDatabaseName("idx_domain_active");
            entity.HasIndex(e => e.PublishesEvents).HasDatabaseName("idx_domain_publishes");
            entity.HasIndex(e => e.ConsumesEvents).HasDatabaseName("idx_domain_consumes");
        });

        return modelBuilder;
    }
}
