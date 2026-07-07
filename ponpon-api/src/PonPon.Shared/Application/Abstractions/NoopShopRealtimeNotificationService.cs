namespace PonPon.Shared.Application.Abstractions;

public sealed class NoopShopRealtimeNotificationService : IShopRealtimeNotificationService
{
    public Task NotifyAsync(ShopRealtimeNotification notification, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
