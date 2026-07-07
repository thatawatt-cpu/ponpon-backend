using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Domain.Orders;
using PonPon.Modules.Ordering.Domain.Returns;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Ordering.Application.Features.Orders.GetOrders;

public sealed class GetOrdersHandler
{
    private readonly IOrderRepository _orders;

    public GetOrdersHandler(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<IReadOnlyCollection<OrderListItemResponse>> HandleAsync(
        GetOrdersQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var returnRequestStatus = NormalizeReturnRequestStatus(query.ReturnRequestStatus);
        var refundRequestStatus = NormalizeRefundRequestStatus(query.RefundRequestStatus);
        var orders = await _orders.GetAsync(
            query.Keyword,
            query.Status?.ToString(),
            query.PaymentStatus?.ToString(),
            returnRequestStatus,
            refundRequestStatus,
            page,
            pageSize,
            cancellationToken);

        return orders.Select(item => new OrderListItemResponse(
            item.Order.Id,
            item.Order.ZortOrderId,
            item.Order.Number,
            item.Order.CustomerName,
            item.Order.CustomerPhone,
            item.Order.Status,
            item.Order.PaymentStatus,
            item.Order.Amount,
            item.Order.PaymentAmount,
            item.Order.ShippingChannel,
            item.Order.TrackingNo,
            item.Order.OrderDate,
            item.Order.SalesChannel,
            item.Order.LastSyncedAt,
            item.ReturnRequestStatus,
            item.Order.OmiseRefundStatus)).ToArray();
    }

    private static string? NormalizeReturnRequestStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;

        return status.Trim().ToLowerInvariant() switch
        {
            "pending" or "requested" => OrderReturnRequestStatus.Requested,
            "approved" => OrderReturnRequestStatus.Approved,
            "rejected" => OrderReturnRequestStatus.Rejected,
            "completed" => OrderReturnRequestStatus.Completed,
            _ => throw new BadRequestException(
                "Return request status must be Pending, Approved, Rejected, or Completed.")
        };
    }

    private static string? NormalizeRefundRequestStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;

        return status.Trim().ToLowerInvariant() switch
        {
            "pending" or "manual_refund_pending" => OrderRefundStatus.ManualRefundPending,
            "refunded" or "completed" or "manual_refunded" => OrderRefundStatus.ManualRefunded,
            _ => throw new BadRequestException(
                "Refund request status must be Pending or Refunded.")
        };
    }
}
