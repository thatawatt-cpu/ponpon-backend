namespace PonPon.Shared.Application.Abstractions;

public interface ILineOrderNotificationService
{
    Task NotifyOrderCreatedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default);
    Task NotifyPaymentCreatedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default);
    Task NotifyPaymentSucceededAsync(LineOrderNotification notification, CancellationToken cancellationToken = default);
    Task NotifyPaymentExpiredAsync(LineOrderNotification notification, CancellationToken cancellationToken = default);
    Task NotifyAutoRefundCompletedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default);
    Task NotifyManualRefundRequestedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default);
    Task NotifyManualRefundCompletedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default);
    Task NotifyPackedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default);
    Task NotifyShippingBookedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default);
    Task NotifyShippingStatusAsync(LineOrderNotification notification, CancellationToken cancellationToken = default);
    Task NotifyReturnRequestedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default);
    Task NotifyReturnUpdatedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default);
    Task NotifyReturnRefundCompletedAsync(LineOrderNotification notification, CancellationToken cancellationToken = default);
}

public sealed record LineOrderNotification(
    Guid OrderId,
    string OrderNumber,
    string? LineUserId,
    string? CustomerName,
    decimal Amount,
    string? Status = null,
    string? PaymentMethod = null,
    string? TrackingNumber = null,
    string? Reason = null,
    DateTime? ExpiresAtUtc = null,
    string? ActionUrl = null,
    string? ShippingAddress = null);
