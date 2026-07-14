namespace PonPon.Shared.Application.Abstractions;

public sealed class NoopLineOrderNotificationService : ILineOrderNotificationService
{
    public Task NotifyOrderCreatedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyPaymentCreatedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyPaymentSucceededAsync(LineOrderNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyPaymentExpiredAsync(LineOrderNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyOrderCancelledAsync(LineOrderNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyAutoRefundCompletedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyManualRefundRequestedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyManualRefundCompletedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyPackedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyShippingBookedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyShippingStatusAsync(LineOrderNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyReturnRequestedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyReturnUpdatedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyReturnRefundCompletedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
