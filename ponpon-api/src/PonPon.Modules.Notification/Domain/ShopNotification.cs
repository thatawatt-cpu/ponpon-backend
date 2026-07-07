using PonPon.Shared.Domain;

namespace PonPon.Modules.Notification.Domain;

public sealed class ShopNotification : Entity, IAuditableEntity
{
    private ShopNotification()
    {
        Type = string.Empty;
        OrderNumber = string.Empty;
        Title = string.Empty;
        Message = string.Empty;
    }

    public Guid? CustomerId { get; private set; }
    public string? LineUserId { get; private set; }
    public string Type { get; private set; }
    public Guid OrderId { get; private set; }
    public string OrderNumber { get; private set; }
    public string Title { get; private set; }
    public string Message { get; private set; }
    public decimal? Amount { get; private set; }
    public string? Status { get; private set; }
    public string? TrackingNumber { get; private set; }
    public string? ActionUrl { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public bool IsRead => ReadAtUtc.HasValue;

    public static ShopNotification Create(
        Guid? customerId,
        string? lineUserId,
        string type,
        Guid orderId,
        string orderNumber,
        string title,
        string message,
        decimal? amount,
        string? status,
        string? trackingNumber,
        string? actionUrl,
        DateTime now)
    {
        return new ShopNotification
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            LineUserId = string.IsNullOrWhiteSpace(lineUserId) ? null : lineUserId.Trim(),
            Type = type.Trim(),
            OrderId = orderId,
            OrderNumber = orderNumber.Trim(),
            Title = title.Trim(),
            Message = message.Trim(),
            Amount = amount,
            Status = string.IsNullOrWhiteSpace(status) ? null : status.Trim(),
            TrackingNumber = string.IsNullOrWhiteSpace(trackingNumber) ? null : trackingNumber.Trim(),
            ActionUrl = string.IsNullOrWhiteSpace(actionUrl) ? null : actionUrl.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void MarkRead(DateTime now)
    {
        if (ReadAtUtc.HasValue)
        {
            return;
        }

        ReadAtUtc = now;
        UpdatedAtUtc = now;
    }
}
