using System.Text.Json;
using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Application.Features.Orders.GetMyOrderById;
using PonPon.Modules.Ordering.Domain.Orders;
using PonPon.Modules.Ordering.Domain.Returns;
using PonPon.Shared.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Ordering.Application.Features.Orders.GetMyOrders;

public sealed class GetMyOrdersHandler
{
    private readonly IOrderRepository _orders;
    private readonly IProductRepository _products;
    private readonly ICurrentUser _currentUser;

    public GetMyOrdersHandler(IOrderRepository orders, IProductRepository products, ICurrentUser currentUser)
    {
        _orders = orders;
        _products = products;
        _currentUser = currentUser;
    }

    public async Task<MyOrdersPagedResponse> HandleAsync(
        GetMyOrdersQuery query,
        CancellationToken cancellationToken = default)
    {
        var customerId = GetCustomerId();
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var filter = ParseFilter(query.Filter);
        var statusFilter = ParseStatusFilter<ZortOrderStatus>(query.Status);
        var paymentStatusFilter = ParseStatusFilter<ZortPaymentStatus>(query.PaymentStatus);

        var projection = await _orders.GetCustomerOrdersAsync(
            customerId, statusFilter, paymentStatusFilter, filter, page, pageSize, cancellationToken);

        // collect SKUs ที่ยังไม่มี imageUrl หรือ options เพื่อ enrich จาก catalog
        var skusToEnrich = projection.Items
            .SelectMany(p => p.ItemsPreview)
            .Where(i => i.Item.ImageUrl is null && i.Item.OptionsJson is null)
            .Select(i => i.Item.Sku)
            .ToHashSet();

        var catalogLookup = skusToEnrich.Count > 0
            ? await _products.GetVariantImageAndOptionsBySkusAsync(skusToEnrich, cancellationToken)
            : (IReadOnlyDictionary<string, (string? ImageUrl, string? OptionsJson)>)new Dictionary<string, (string?, string?)>();

        var items = projection.Items.Select(p => new MyOrderListItemResponse(
            p.Order.Id,
            p.Order.Number,
            p.Order.Status,
            p.Order.PaymentStatus,
            p.Order.Amount,
            p.Order.PaymentAmount,
            p.Order.ShippingChannel,
            p.Order.TrackingNo,
            p.Order.OrderDate,
            p.Order.ReceivedAtUtc,
            p.ReturnRequestStatus,
            p.Order.OmiseRefundStatus,
            GetReturnRefundStatus(p.ReturnRequestStatus, p.Order.OmiseRefundStatus),
            GetReturnRefundText(p.ReturnRequestStatus, p.Order.OmiseRefundStatus),
            p.ItemsCount,
            p.ItemsPreview.Select(preview =>
            {
                var item = preview.Item;
                var imageUrl = item.ImageUrl;
                var optionsJson = item.OptionsJson;
                if (imageUrl is null && catalogLookup.TryGetValue(item.Sku, out var cat))
                {
                    imageUrl = cat.ImageUrl;
                    optionsJson ??= cat.OptionsJson;
                }

                return new MyOrderListItemPreviewResponse(
                    item.Id,
                    item.ProductId,
                    item.VariantId,
                    item.Sku,
                    item.Name,
                    (int)item.Quantity,
                    item.TotalPrice,
                    imageUrl,
                    preview.ReviewId,
                    preview.ReviewId.HasValue,
                    optionsJson is not null
                        ? JsonSerializer.Deserialize<MyOrderItemOptionResponse[]>(optionsJson) ?? []
                        : []);
            }).ToArray())).ToArray();

        return new MyOrdersPagedResponse(items, page, pageSize, projection.Total, page * pageSize < projection.Total);
    }

    private static string? GetReturnRefundStatus(string? returnRequestStatus, string? omiseRefundStatus)
    {
        if (string.IsNullOrWhiteSpace(returnRequestStatus) && string.IsNullOrWhiteSpace(omiseRefundStatus))
        {
            return null;
        }

        return IsReturnRefundCompleted(returnRequestStatus, omiseRefundStatus)
            ? "completed"
            : "pending";
    }

    private static string? GetReturnRefundText(string? returnRequestStatus, string? omiseRefundStatus)
    {
        if (string.IsNullOrWhiteSpace(returnRequestStatus) && string.IsNullOrWhiteSpace(omiseRefundStatus))
        {
            return null;
        }

        return IsReturnRefundCompleted(returnRequestStatus, omiseRefundStatus)
            ? "สำเร็จ"
            : "รอพิจารณา";
    }

    private static bool IsReturnRefundCompleted(string? returnRequestStatus, string? omiseRefundStatus)
        => string.Equals(returnRequestStatus, OrderReturnRequestStatus.Completed, StringComparison.OrdinalIgnoreCase)
           || string.Equals(omiseRefundStatus, OrderRefundStatus.ManualRefunded, StringComparison.OrdinalIgnoreCase)
           || string.Equals(omiseRefundStatus, "closed", StringComparison.OrdinalIgnoreCase);

    private Guid GetCustomerId()
    {
        if (!_currentUser.IsAuthenticated
            || _currentUser.UserType != "Customer"
            || _currentUser.CustomerId is not Guid customerId)
        {
            throw new UnauthorizedException("Customer authentication is required.");
        }

        return customerId;
    }

    private static IReadOnlyCollection<string>? ParseStatusFilter<TEnum>(string? value)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var statuses = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => Enum.TryParse<TEnum>(x, ignoreCase: true, out var parsed)
                ? parsed.ToString()
                : null)
            .Where(x => x is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return statuses.Length == 0 ? null : statuses;
    }

    private static MyOrderFilter? ParseFilter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim().ToLowerInvariant() switch
        {
            "pending_payment" or "pending-payment" => MyOrderFilter.PendingPayment,
            "preparing" or "prepare" or "paid" => MyOrderFilter.Preparing,
            "awaiting_receive" or "awaiting-receive" => MyOrderFilter.AwaitingReceive,
            "completed" => MyOrderFilter.Completed,
            "cancelled" or "canceled" => MyOrderFilter.Cancelled,
            "return_refund" or "return-refund" => MyOrderFilter.ReturnRefund,
            "awaiting_review" or "awaiting-review" => MyOrderFilter.AwaitingReview,
            _ => throw new BadRequestException(
                "Order filter must be pending_payment, preparing, awaiting_receive, completed, awaiting_review, cancelled, or return_refund.")
        };
    }
}
