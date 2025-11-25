using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Fabrica.Domain.Esb.Interfaces;
using Fabrica.Domain.Esb.Models;

namespace Fabrica.Domain.Esb.Interceptors;

/// <summary>
/// EF Core interceptor that captures entity changes and writes them to the outbox table
/// in the same transaction as the original operation.
/// Uses cdc.outbox_config to determine which tables/actions to capture.
/// </summary>
public class OutboxSaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    // Cache for outbox configuration, keyed by "schema.table"
    private static readonly ConcurrentDictionary<string, OutboxConfigEntry> ConfigCache = new();
    private static DateTime _lastConfigRefresh = DateTime.MinValue;
    private static readonly TimeSpan CacheRefreshInterval = TimeSpan.FromMinutes(5);
    private static readonly object ConfigLock = new();

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            AddOutboxEvents(eventData.Context);
        }
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            AddOutboxEvents(eventData.Context);
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddOutboxEvents(DbContext context)
    {
        // Refresh config cache if needed
        RefreshConfigCacheIfNeeded(context);

        var outboxEvents = new List<OutboxEvent>();

        // Get all tracked entities that implement IOutboxEntity
        var entries = context.ChangeTracker
            .Entries<IOutboxEntity>()
            .Where(e => e.State == EntityState.Added ||
                        e.State == EntityState.Modified ||
                        e.State == EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            // Check if this entity/action should be captured based on config
            if (ShouldCaptureEvent(entry))
            {
                var outboxEvent = CreateOutboxEvent(entry);
                if (outboxEvent is not null)
                {
                    outboxEvents.Add(outboxEvent);
                }
            }
        }

        // Add all outbox events to the context
        if (outboxEvents.Count > 0)
        {
            var outboxSet = context.Set<OutboxEvent>();
            outboxSet.AddRange(outboxEvents);
        }
    }

    private void RefreshConfigCacheIfNeeded(DbContext context)
    {
        if (DateTime.UtcNow - _lastConfigRefresh < CacheRefreshInterval)
            return;

        lock (ConfigLock)
        {
            // Double-check after acquiring lock
            if (DateTime.UtcNow - _lastConfigRefresh < CacheRefreshInterval)
                return;

            try
            {
                var configSet = context.Set<OutboxConfig>();
                var configs = configSet.Where(c => c.IsActive).ToList();

                ConfigCache.Clear();
                foreach (var config in configs)
                {
                    var key = $"{config.SchemaName}.{config.TableName}".ToLowerInvariant();
                    ConfigCache[key] = new OutboxConfigEntry
                    {
                        CaptureInsert = config.CaptureInsert,
                        CaptureUpdate = config.CaptureUpdate,
                        CaptureDelete = config.CaptureDelete
                    };
                }

                _lastConfigRefresh = DateTime.UtcNow;
            }
            catch (Exception)
            {
                // If config table doesn't exist or query fails, cache will be empty
                // which means no events will be captured (fail closed)
                ConfigCache.Clear();
                _lastConfigRefresh = DateTime.UtcNow;
            }
        }
    }

    private bool ShouldCaptureEvent(EntityEntry<IOutboxEntity> entry)
    {
        var entityType = entry.Metadata.ClrType;
        var (schemaName, tableName) = GetTableInfo(entityType);
        var key = $"{schemaName}.{tableName}".ToLowerInvariant();

        if (!ConfigCache.TryGetValue(key, out var config))
        {
            // No config found for this table - don't capture
            return false;
        }

        return entry.State switch
        {
            EntityState.Added => config.CaptureInsert,
            EntityState.Modified => config.CaptureUpdate,
            EntityState.Deleted => config.CaptureDelete,
            _ => false
        };
    }

    private static (string schema, string table) GetTableInfo(Type entityType)
    {
        // Try to get schema and table from TableAttribute
        var tableAttribute = entityType
            .GetCustomAttributes(typeof(TableAttribute), true)
            .FirstOrDefault() as TableAttribute;

        if (tableAttribute != null)
        {
            return (tableAttribute.Schema ?? "public", tableAttribute.Name);
        }

        // Fall back to convention: use type name in snake_case
        return ("fabrica", ToSnakeCase(entityType.Name));
    }

    private OutboxEvent? CreateOutboxEvent(EntityEntry<IOutboxEntity> entry)
    {
        var entity = entry.Entity;
        var entityType = entry.Metadata.ClrType;
        var aggregateType = GetAggregateType(entityType);
        var eventType = GetEventType(aggregateType, entry.State);

        // For deleted entities, capture the state before deletion
        var eventData = entry.State == EntityState.Deleted
            ? SerializeOriginalValues(entry)
            : SerializeEntity(entity);

        return new OutboxEvent
        {
            Id = Guid.NewGuid(),
            TenantId = entity.TenantId,
            AggregateType = aggregateType,
            AggregateId = entity.Id,
            EventType = eventType,
            EventData = eventData,
            CreatedAt = DateTime.UtcNow,
            Status = "pending"
        };
    }

    private static string GetAggregateType(Type entityType)
    {
        // Check for TableAttribute first
        var tableAttribute = entityType
            .GetCustomAttributes(typeof(TableAttribute), true)
            .FirstOrDefault() as TableAttribute;

        if (tableAttribute != null)
        {
            return tableAttribute.Name;
        }

        // Convert "Product" -> "product", "ProductCategory" -> "product_category"
        return ToSnakeCase(entityType.Name);
    }

    private static string GetEventType(string aggregateType, EntityState state)
    {
        var action = state switch
        {
            EntityState.Added => "created",
            EntityState.Modified => "updated",
            EntityState.Deleted => "deleted",
            _ => "unknown"
        };
        return $"{aggregateType}.{action}";
    }

    private static string SerializeEntity(IOutboxEntity entity)
    {
        try
        {
            return JsonSerializer.Serialize(entity, entity.GetType(), JsonOptions);
        }
        catch (Exception ex)
        {
            // If serialization fails, return minimal data
            return JsonSerializer.Serialize(new
            {
                id = entity.Id,
                tenantId = entity.TenantId,
                error = $"Serialization failed: {ex.Message}"
            }, JsonOptions);
        }
    }

    private static string SerializeOriginalValues(EntityEntry entry)
    {
        try
        {
            var originalValues = new Dictionary<string, object?>();
            foreach (var property in entry.OriginalValues.Properties)
            {
                originalValues[ToSnakeCase(property.Name)] = entry.OriginalValues[property];
            }
            return JsonSerializer.Serialize(originalValues, JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                error = $"Serialization failed: {ex.Message}"
            }, JsonOptions);
        }
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new System.Text.StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                    result.Append('_');
                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append(c);
            }
        }
        return result.ToString();
    }

    /// <summary>
    /// Force a refresh of the configuration cache on the next operation.
    /// Useful after updating the outbox_config table.
    /// </summary>
    public static void InvalidateConfigCache()
    {
        lock (ConfigLock)
        {
            _lastConfigRefresh = DateTime.MinValue;
        }
    }

    private class OutboxConfigEntry
    {
        public bool CaptureInsert { get; set; }
        public bool CaptureUpdate { get; set; }
        public bool CaptureDelete { get; set; }
    }
}
