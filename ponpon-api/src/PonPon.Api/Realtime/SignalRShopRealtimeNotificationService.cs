using Microsoft.AspNetCore.SignalR;
using PonPon.Modules.Notification.Domain;
using PonPon.Modules.Notification.Infrastructure.Persistence;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Api.Realtime;

public sealed class SignalRShopRealtimeNotificationService : IShopRealtimeNotificationService
{
    private readonly IHubContext<ShopNotificationHub> _hubContext;
    private readonly NotificationDbContext _dbContext;
    private readonly ILogger<SignalRShopRealtimeNotificationService> _logger;

    public SignalRShopRealtimeNotificationService(
        IHubContext<ShopNotificationHub> hubContext,
        NotificationDbContext dbContext,
        ILogger<SignalRShopRealtimeNotificationService> logger)
    {
        _hubContext = hubContext;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task NotifyAsync(
        ShopRealtimeNotification notification,
        CancellationToken cancellationToken = default)
    {
        var groups = GetTargetGroups(notification).Distinct(StringComparer.Ordinal).ToArray();
        if (groups.Length == 0)
        {
            _logger.LogInformation(
                "Shop realtime notification skipped because no target was available: Type={Type} OrderNumber={OrderNumber}",
                notification.Type,
                notification.OrderNumber);
            return;
        }

        var now = notification.CreatedAtUtc ?? DateTime.UtcNow;
        var savedNotification = ShopNotification.Create(
            notification.CustomerId,
            notification.LineUserId,
            notification.Type,
            notification.OrderId,
            notification.OrderNumber,
            notification.Title,
            notification.Message,
            notification.Amount,
            notification.Status,
            notification.TrackingNumber,
            notification.ActionUrl,
            now);

        await _dbContext.ShopNotifications.AddAsync(savedNotification, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var payload = new
        {
            savedNotification.Id,
            notification.Type,
            notification.OrderId,
            notification.OrderNumber,
            notification.Title,
            notification.Message,
            notification.Amount,
            notification.Status,
            notification.TrackingNumber,
            notification.ActionUrl,
            IsRead = false,
            ReadAtUtc = (DateTime?)null,
            CreatedAtUtc = now
        };

        foreach (var group in groups)
        {
            await _hubContext.Clients
                .Group(group)
                .SendAsync("shopNotification", payload, cancellationToken);
        }
    }

    private static IEnumerable<string> GetTargetGroups(ShopRealtimeNotification notification)
    {
        if (notification.CustomerId is Guid customerId)
            yield return ShopNotificationGroups.Customer(customerId);

        if (!string.IsNullOrWhiteSpace(notification.LineUserId))
            yield return ShopNotificationGroups.LineUser(notification.LineUserId);
    }
}
