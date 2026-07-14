using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Ordering.Application.Features.Orders.GetOrderById;

public sealed class GetOrderByIdHandler
{
    private readonly IOrderRepository _orders;
    private readonly IProductRepository _products;

    public GetOrderByIdHandler(
        IOrderRepository orders,
        IProductRepository products)
    {
        _orders = orders;
        _products = products;
    }

    public async Task<OrderDetailResponse> HandleAsync(
        GetOrderByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetByIdAsync(query.Id, cancellationToken)
            ?? throw new NotFoundException("Order was not found.");

        var skusWithoutImages = order.Items
            .Where(x => string.IsNullOrWhiteSpace(x.ImageUrl))
            .Select(x => x.Sku)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var catalogItems = skusWithoutImages.Count > 0
            ? await _products.GetVariantImageAndOptionsBySkusAsync(
                skusWithoutImages,
                cancellationToken)
            : new Dictionary<string, (string? ImageUrl, string? OptionsJson)>();
        var amount = CalculateGrossItemAmount(order);
        var discountAmount = CalculateTotalDiscountAmount(order);
        var paymentAmount = CalculatePaymentAmount(
            amount,
            discountAmount,
            order.ShippingAmount,
            order.VatAmount);

        return new OrderDetailResponse(
            order.Id,
            order.ZortOrderId,
            order.Number,
            order.ZortCustomerId,
            order.CustomerCode,
            order.CustomerName,
            order.CustomerIdNumber,
            order.CustomerEmail,
            order.CustomerPhone,
            order.CustomerAddress,
            order.Status,
            order.PaymentStatus,
            amount,
            order.VatAmount,
            order.ShippingAmount,
            paymentAmount,
            discountAmount,
            order.ShippingChannel,
            order.ShippingName,
            order.ShippingAddress,
            order.ShippingPhone,
            order.TrackingNo,
            order.OrderDate,
            order.ShippingDate,
            order.Reference,
            order.Description,
            order.SalesChannel,
            order.IntegrationCustomerId,
            order.IntegrationCustomer,
            order.WarehouseCode,
            order.IsCod,
            order.Currency,
            order.CancellationReason,
            order.CanceledBy,
            order.CanceledAtUtc,
            order.OmiseRefundId,
            order.OmiseRefundStatus,
            order.RefundedAmount,
            order.ZortCreatedAt,
            order.ZortUpdatedAt,
            order.LastSyncedAt,
            order.PricingSnapshotJson,
            order.Items.Select(x => new OrderItemResponse(
                x.Id,
                x.ZortProductId,
                x.Sku,
                x.Name,
                x.Quantity,
                x.UnitText,
                x.PricePerUnit,
                x.Discount,
                x.DiscountAmount,
                x.TotalPrice,
                x.ProductType,
                x.BundleId,
                x.BundleCode,
                x.BundleName,
                !string.IsNullOrWhiteSpace(x.ImageUrl)
                    ? x.ImageUrl
                    : catalogItems.GetValueOrDefault(x.Sku).ImageUrl)).ToArray(),
            order.Payments.Select(x => new OrderPaymentResponse(
                x.Id,
                x.ZortPaymentId,
                x.Name,
                x.Amount,
                x.PaymentDateTime)).ToArray());
    }

    private static decimal CalculateGrossItemAmount(Domain.Orders.Order order)
        => order.Items.Sum(x => x.TotalPrice + x.DiscountAmount);

    private static decimal CalculateTotalDiscountAmount(Domain.Orders.Order order)
        => order.DiscountAmount + order.Items.Sum(x => x.DiscountAmount);

    private static decimal CalculatePaymentAmount(
        decimal amount,
        decimal discountAmount,
        decimal shippingAmount,
        decimal vatAmount)
        => Math.Max(
            0,
            decimal.Round(
                amount - discountAmount + shippingAmount + vatAmount,
                2,
                MidpointRounding.AwayFromZero));
}
