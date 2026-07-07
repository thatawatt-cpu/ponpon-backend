using System.Text.Json;
using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Application.Features.Orders.GetMyOrderById;
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
        var statusFilter = ParseStatusFilter<ZortOrderStatus>(query.Status);
        var paymentStatusFilter = ParseStatusFilter<ZortPaymentStatus>(query.PaymentStatus);

        var projection = await _orders.GetCustomerOrdersAsync(
            customerId, statusFilter, paymentStatusFilter, page, pageSize, cancellationToken);

        // collect SKUs ที่ยังไม่มี imageUrl หรือ options เพื่อ enrich จาก catalog
        var skusToEnrich = projection.Items
            .SelectMany(p => p.ItemsPreview)
            .Where(i => i.ImageUrl is null && i.OptionsJson is null)
            .Select(i => i.Sku)
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
            p.ItemsCount,
            p.ItemsPreview.Select(item =>
            {
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
                    optionsJson is not null
                        ? JsonSerializer.Deserialize<MyOrderItemOptionResponse[]>(optionsJson) ?? []
                        : []);
            }).ToArray())).ToArray();

        return new MyOrdersPagedResponse(items, page, pageSize, projection.Total, page * pageSize < projection.Total);
    }

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
}
