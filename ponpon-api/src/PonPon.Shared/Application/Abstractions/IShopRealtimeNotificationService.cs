namespace PonPon.Shared.Application.Abstractions;

public interface IShopRealtimeNotificationService
{
    Task NotifyAsync(ShopRealtimeNotification notification, CancellationToken cancellationToken = default);
}

public sealed record ShopRealtimeNotification(
    Guid? CustomerId,
    string? LineUserId,
    string Type,
    Guid OrderId,
    string OrderNumber,
    string Title,
    string Message,
    decimal? Amount = null,
    string? Status = null,
    string? TrackingNumber = null,
    string? ActionUrl = null,
    DateTime? CreatedAtUtc = null);
