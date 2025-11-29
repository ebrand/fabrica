using Microsoft.AspNetCore.SignalR;

namespace AdminBFF.Hubs;

/// <summary>
/// SignalR hub for broadcasting ESB telemetry events to connected clients.
/// Clients can subscribe to receive real-time producer and consumer telemetry.
/// </summary>
public class TelemetryHub : Hub
{
    private readonly ILogger<TelemetryHub> _logger;

    public TelemetryHub(ILogger<TelemetryHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected to TelemetryHub: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected from TelemetryHub: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Allows clients to subscribe to specific domain telemetry.
    /// </summary>
    public async Task SubscribeToDomain(string domain)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"domain:{domain}");
        _logger.LogDebug("Client {ConnectionId} subscribed to domain: {Domain}", Context.ConnectionId, domain);
    }

    /// <summary>
    /// Allows clients to unsubscribe from specific domain telemetry.
    /// </summary>
    public async Task UnsubscribeFromDomain(string domain)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"domain:{domain}");
        _logger.LogDebug("Client {ConnectionId} unsubscribed from domain: {Domain}", Context.ConnectionId, domain);
    }
}
