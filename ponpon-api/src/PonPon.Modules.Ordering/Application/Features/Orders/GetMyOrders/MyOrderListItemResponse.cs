using PonPon.Modules.Ordering.Application.Features.Orders.GetMyOrderById;

namespace PonPon.Modules.Ordering.Application.Features.Orders.GetMyOrders;

public sealed record MyOrdersPagedResponse(
    IReadOnlyCollection<MyOrderListItemResponse> Items,
    int Page,
    int PageSize,
    int Total,
    bool HasMore);

public sealed record MyOrderListItemResponse(
    Guid Id,
    string Number,
    string Status,
    string PaymentStatus,
    decimal Amount,
    decimal PaymentAmount,
    string? ShippingChannel,
    string? TrackingNo,
    DateTime? OrderDate,
    int ItemsCount,
    IReadOnlyCollection<MyOrderListItemPreviewResponse> ItemsPreview);

public sealed record MyOrderListItemPreviewResponse(
    Guid Id,
    Guid? ProductId,
    Guid? VariantId,
    string Sku,
    string Name,
    int Quantity,
    decimal TotalPrice,
    string? ImageUrl,
    IReadOnlyCollection<MyOrderItemOptionResponse> Options);
