using Microsoft.Extensions.Logging;
using PonPon.Modules.Ordering.Application;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Domain.Orders;
using PonPon.Modules.Ordering.Infrastructure.ExternalServices.Zort;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Ordering.Application.Features.Orders.SyncPendingOrderToZort;

public sealed class SyncPendingOrderToZortHandler
{
    private readonly IOrderRepository _orders;
    private readonly IZortOrderClient _zort;
    private readonly IOrderingUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<SyncPendingOrderToZortHandler> _logger;

    public SyncPendingOrderToZortHandler(
        IOrderRepository orders,
        IZortOrderClient zort,
        IOrderingUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        ILogger<SyncPendingOrderToZortHandler> logger)
    {
        _orders = orders;
        _zort = zort;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task HandleAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            _logger.LogWarning("ZORT order sync skipped because local order {OrderId} was not found", orderId);
            return;
        }

        await HandleAsync(order, cancellationToken);
    }

    public async Task HandleAsync(Order order, CancellationToken cancellationToken = default)
    {
        if (order.ZortOrderId > 0)
            return;

        if (string.Equals(order.Status, "Voided", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("ZORT order sync skipped for voided order {OrderNumber}", order.Number);
            return;
        }

        var request = CreateZortAddOrderRequest(order);
        var zortOrderId = await _zort.AddOrderAsync(request, cancellationToken);

        order.ApplyZortSnapshot(CreateSyncedSnapshot(order, zortOrderId), _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Local order {OrderNumber} synced to ZORT order {ZortOrderId}",
            order.Number, zortOrderId);

        if (IsPaid(order.PaymentStatus))
        {
            await SyncPaidStateToZortAsync(order, cancellationToken);
        }
    }

    private async Task SyncPaidStateToZortAsync(Order order, CancellationToken cancellationToken)
    {
        try
        {
            var amount = order.PaymentAmount > 0 ? order.PaymentAmount : order.Amount;
            await _zort.UpdateOrderPaymentAsync(
                order.Number,
                amount,
                "Online Payment",
                null,
                cancellationToken);
            await _zort.UpdateOrderStatusAsync(
                order.Number,
                (int)ZortOrderStatus.Waiting,
                cancellationToken);

            _logger.LogInformation("Paid local order {OrderNumber} synced to ZORT payment/status", order.Number);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync paid state to ZORT for order {OrderNumber}", order.Number);
        }
    }

    private static ZortAddOrderRequest CreateZortAddOrderRequest(Order order)
    {
        return new ZortAddOrderRequest(
            order.Number,
            order.Reference ?? order.Number.Replace("LIFF-", string.Empty, StringComparison.OrdinalIgnoreCase),
            order.CustomerName ?? order.ShippingName ?? order.Number,
            order.CustomerEmail,
            order.CustomerPhone ?? order.ShippingPhone ?? string.Empty,
            order.CustomerAddress ?? order.ShippingAddress ?? string.Empty,
            order.ShippingName ?? order.CustomerName ?? order.Number,
            order.ShippingPhone ?? order.CustomerPhone ?? string.Empty,
            order.ShippingAddress ?? order.CustomerAddress ?? string.Empty,
            order.ShippingChannel,
            order.ShippingAmount,
            order.Description,
            order.SalesChannel,
            order.Amount,
            order.VatAmount,
            order.DiscountAmount,
            order.Items.Select(x => new ZortAddOrderItemRequest(
                x.Sku,
                x.Name,
                (int)x.Quantity,
                x.PricePerUnit,
                x.DiscountAmount,
                x.TotalPrice)).ToArray());
    }

    private static OrderSnapshot CreateSyncedSnapshot(Order order, long zortOrderId)
    {
        return new OrderSnapshot(
            zortOrderId,
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
            order.Amount,
            order.VatAmount,
            order.ShippingAmount,
            order.PaymentAmount,
            order.DiscountAmount,
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
            order.TagsJson,
            order.ZortCreatedAt,
            order.ZortUpdatedAt,
            order.Items.Select(x => new OrderItemSnapshot(
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
                x.RawZortJson,
                x.ProductId,
                x.VariantId,
                x.ImageUrl,
                x.OptionsJson)).ToArray(),
            order.Payments.Select(x => new OrderPaymentSnapshot(
                x.ZortPaymentId,
                x.Name,
                x.Amount,
                x.PaymentDateTime,
                x.RawZortJson)).ToArray(),
            order.RawZortJson);
    }

    private static bool IsPaid(string paymentStatus)
        => string.Equals(paymentStatus, "Paid", StringComparison.OrdinalIgnoreCase)
           || string.Equals(paymentStatus, ((int)ZortPaymentStatus.Paid).ToString(), StringComparison.OrdinalIgnoreCase);
}
