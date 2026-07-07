namespace PonPon.Modules.Ordering.Application.Features.Orders.GetOrders;

public sealed record GetOrdersQuery(
    string? Keyword,
    ZortOrderStatus? Status,
    ZortPaymentStatus? PaymentStatus,
    string? ReturnRequestStatus,
    string? RefundRequestStatus,
    int Page = 1,
    int PageSize = 20);
