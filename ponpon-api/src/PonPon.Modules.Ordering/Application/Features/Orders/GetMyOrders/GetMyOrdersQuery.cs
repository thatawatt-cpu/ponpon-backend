namespace PonPon.Modules.Ordering.Application.Features.Orders.GetMyOrders;

public sealed record GetMyOrdersQuery(
    string? Status,
    string? PaymentStatus,
    int Page = 1,
    int PageSize = 20);
