namespace PonPon.Shared.Application.Abstractions;

public interface IOrderCancellationNotifier
{
    Task NotifyCustomerCancellationCompletedAsync(
        OrderCancellationNotification notification,
        CancellationToken cancellationToken = default);

    Task NotifyCustomerCancellationRequiresManualRefundAsync(
        OrderCancellationNotification notification,
        CancellationToken cancellationToken = default);
}

public sealed record OrderCancellationNotification(
    Guid OrderId,
    string OrderNumber,
    string? CustomerName,
    string? CustomerPhone,
    decimal PaymentAmount,
    string PaymentStatus,
    string? OmiseChargeId,
    string Reason);
