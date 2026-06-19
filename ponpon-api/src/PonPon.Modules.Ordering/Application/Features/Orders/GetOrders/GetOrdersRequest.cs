namespace PonPon.Modules.Ordering.Application.Features.Orders.GetOrders;

public sealed record GetOrdersRequest(
    string? Keyword,
    string? Status,
    string? PaymentStatus,
    int Page = 1,
    int PageSize = 20);
