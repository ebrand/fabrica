namespace Fabrica.Domain.Esb.Interfaces;

/// <summary>
/// Interface that entities must implement to be tracked by the outbox interceptor.
/// Entities implementing this interface will automatically have their changes
/// recorded in the cdc.outbox table when SaveChanges is called.
/// </summary>
public interface IOutboxEntity
{
    /// <summary>
    /// The unique identifier of the entity (aggregate ID)
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// The tenant ID for multi-tenant support
    /// </summary>
    string TenantId { get; }
}
