namespace PonPon.Shared.Application.Abstractions;

public sealed class NoopOrderCancellationNotifier : IOrderCancellationNotifier
{
    public Task NotifyCustomerCancellationCompletedAsync(
        OrderCancellationNotification notification,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task NotifyCustomerCancellationRequiresManualRefundAsync(
        OrderCancellationNotification notification,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
