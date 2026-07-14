namespace PonPon.Modules.Ordering.Application.Features.Orders.GetOrders;

public sealed record OrderListResponse(
    IReadOnlyCollection<OrderListItemResponse> Items,
    int Total,
    int Page,
    int PageSize,
    IReadOnlyDictionary<string, int> StatusCounts,
    DateTime? LastSuccessfulSyncAt);

public sealed record OrderListItemResponse(
    Guid Id,
    long ZortOrderId,
    string Number,
    string? CustomerName,
    string? CustomerPhone,
    string Status,
    string PaymentStatus,
    decimal Amount,
    decimal PaymentAmount,
    string? ShippingChannel,
    string? TrackingNo,
    DateTime? OrderDate,
    string SalesChannel,
    DateTime LastSyncedAt,
    string? ReturnRequestStatus,
    string? RefundRequestStatus,
    IReadOnlyCollection<string> AllowedActions,
    bool CanCancel);
