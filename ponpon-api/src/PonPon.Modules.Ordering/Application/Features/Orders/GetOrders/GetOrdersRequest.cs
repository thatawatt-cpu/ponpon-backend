namespace PonPon.Modules.Ordering.Application.Features.Orders.GetOrders;

public sealed record GetOrdersRequest(
    string? Keyword,
    string? Status,
    string? PaymentStatus,
    string? ReturnRequestStatus,
    string? RefundRequestStatus,
    DateTime? DateFrom,
    DateTime? DateTo,
    string? ShippingChannel,
    string? SalesChannel,
    string? SortBy,
    string? SortDirection,
    int Page = 1,
    int PageSize = 20);
