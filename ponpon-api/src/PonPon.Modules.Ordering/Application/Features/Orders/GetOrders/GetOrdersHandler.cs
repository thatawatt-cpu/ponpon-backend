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
        var filters = NormalizeAdminOrderFilters(query);
        var orders = await _orders.GetAsync(
            query.Keyword,
            filters.Status,
            filters.PaymentStatus,
            filters.ReturnRequestStatus,
            filters.RefundRequestStatus,
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

    private static AdminOrderFilters NormalizeAdminOrderFilters(GetOrdersQuery query)
    {
        var status = NormalizeFilter(query.Status);
        var paymentStatus = NormalizePaymentStatus(query.PaymentStatus);
        var returnRequestStatus = NormalizeReturnRequestStatus(query.ReturnRequestStatus);
        var refundRequestStatus = NormalizeRefundRequestStatus(query.RefundRequestStatus);

        if (status is null)
        {
            return new AdminOrderFilters(null, paymentStatus, returnRequestStatus, refundRequestStatus);
        }

        return status.Trim().ToLowerInvariant() switch
        {
            "all" => new AdminOrderFilters(null, paymentStatus, returnRequestStatus, refundRequestStatus),
            "pending_payment" => new AdminOrderFilters(
                null,
                paymentStatus ?? ZortPaymentStatus.Pending.ToString(),
                returnRequestStatus,
                refundRequestStatus),
            "paid" or "packing" => new AdminOrderFilters(
                ZortOrderStatus.Waiting.ToString(),
                paymentStatus ?? ZortPaymentStatus.Paid.ToString(),
                returnRequestStatus,
                refundRequestStatus),
            "packed" => new AdminOrderFilters(
                ZortOrderStatus.Packed.ToString(),
                paymentStatus ?? ZortPaymentStatus.Paid.ToString(),
                returnRequestStatus,
                refundRequestStatus),
            "shipped" => new AdminOrderFilters(
                ZortOrderStatus.Shipping.ToString(),
                paymentStatus ?? ZortPaymentStatus.Paid.ToString(),
                returnRequestStatus,
                refundRequestStatus),
            "completed" => new AdminOrderFilters(
                ZortOrderStatus.Success.ToString(),
                paymentStatus,
                returnRequestStatus,
                refundRequestStatus),
            "cancelled" or "canceled" => new AdminOrderFilters(
                ZortOrderStatus.Voided.ToString(),
                paymentStatus,
                returnRequestStatus,
                refundRequestStatus),
            "refund_requested" => new AdminOrderFilters(
                null,
                paymentStatus,
                returnRequestStatus ?? OrderReturnRequestStatus.Requested,
                refundRequestStatus ?? OrderRefundStatus.ManualRefundPending),
            "refunded" => new AdminOrderFilters(
                null,
                paymentStatus,
                returnRequestStatus,
                refundRequestStatus ?? OrderRefundStatus.ManualRefunded),
            _ => new AdminOrderFilters(
                NormalizeOrderStatus(status),
                paymentStatus,
                returnRequestStatus,
                refundRequestStatus)
        };
    }

    private static string NormalizeOrderStatus(string status)
    {
        if (Enum.TryParse<ZortOrderStatus>(status, ignoreCase: true, out var named))
        {
            return named.ToString();
        }

        if (int.TryParse(status, out var numeric) && Enum.IsDefined(typeof(ZortOrderStatus), numeric))
        {
            return ((ZortOrderStatus)numeric).ToString();
        }

        throw new BadRequestException(
            "Order status must be one of pending_payment, paid, packing, packed, shipped, completed, cancelled, refund_requested, refunded, or a valid ZORT order status.");
    }

    private static string? NormalizePaymentStatus(string? paymentStatus)
    {
        if (string.IsNullOrWhiteSpace(paymentStatus))
        {
            return null;
        }

        var trimmed = paymentStatus.Trim();
        if (Enum.TryParse<ZortPaymentStatus>(trimmed, ignoreCase: true, out var named))
        {
            return named.ToString();
        }

        if (int.TryParse(trimmed, out var numeric) && Enum.IsDefined(typeof(ZortPaymentStatus), numeric))
        {
            return ((ZortPaymentStatus)numeric).ToString();
        }

        throw new BadRequestException("Payment status must be a valid ZORT payment status.");
    }

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

    private sealed record AdminOrderFilters(
        string? Status,
        string? PaymentStatus,
        string? ReturnRequestStatus,
        string? RefundRequestStatus);
}
