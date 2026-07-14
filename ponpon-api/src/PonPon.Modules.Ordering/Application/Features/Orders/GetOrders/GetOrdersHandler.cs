using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Domain.Orders;
using PonPon.Modules.Ordering.Domain.Returns;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Ordering.Application.Features.Orders.GetOrders;

public sealed class GetOrdersHandler
{
    private readonly IOrderRepository _orders;
    private readonly IOrderSyncRunRepository _syncRuns;

    public GetOrdersHandler(IOrderRepository orders, IOrderSyncRunRepository syncRuns)
    {
        _orders = orders;
        _syncRuns = syncRuns;
    }

    public async Task<OrderListResponse> HandleAsync(
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
            query.DateFrom,
            query.DateTo,
            NormalizeFilter(query.ShippingChannel),
            NormalizeFilter(query.SalesChannel),
            query.SortBy,
            query.SortDirection,
            page,
            pageSize,
            cancellationToken);
        var lastSuccessfulSyncAt = await _syncRuns.GetLastSuccessfulCompletedAtAsync(cancellationToken);

        var items = orders.Items.Select(item => new OrderListItemResponse(
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
            item.Order.OmiseRefundStatus,
            GetAllowedActions(item.Order),
            CanCancel(item.Order))).ToArray();

        return new OrderListResponse(
            items,
            orders.Total,
            page,
            pageSize,
            orders.StatusCounts,
            lastSuccessfulSyncAt);
    }

    private static string? NormalizeFilter(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IReadOnlyCollection<string> GetAllowedActions(Order order)
    {
        var actions = new List<string>();
        if (CanCancel(order))
        {
            actions.Add("cancel");
        }

        if (string.Equals(order.OmiseRefundStatus, OrderRefundStatus.ManualRefundPending, StringComparison.OrdinalIgnoreCase))
        {
            actions.Add("approveManualRefund");
        }

        return actions;
    }

    private static bool CanCancel(Order order)
        => !string.Equals(order.Status, "Voided", StringComparison.OrdinalIgnoreCase)
           && !string.Equals(order.Status, ((int)ZortOrderStatus.Voided).ToString(), StringComparison.OrdinalIgnoreCase);

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
