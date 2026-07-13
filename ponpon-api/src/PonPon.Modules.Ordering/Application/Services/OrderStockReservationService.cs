using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Ordering.Application.Abstractions;
using PonPon.Modules.Ordering.Domain.Orders;
using PonPon.Shared.Application.Abstractions;
using PonPon.Modules.Promotion.Application;

namespace PonPon.Modules.Ordering.Application.Services;

public sealed class OrderStockReservationService
{
    private readonly IProductRepository _products;
    private readonly IOrderRepository _orders;
    private readonly IDateTimeProvider _clock;
    private readonly ICouponService _coupons;
    private readonly IFlashSaleRepository _flashSales;

    public OrderStockReservationService(
        IProductRepository products,
        IOrderRepository orders,
        ICouponService coupons,
        IFlashSaleRepository flashSales,
        IDateTimeProvider clock)
    {
        _products = products;
        _orders = orders;
        _coupons = coupons;
        _flashSales = flashSales;
        _clock = clock;
    }

    public async Task ReleaseAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        if (order.HasStockReservation)
        {
            var now = _clock.UtcNow;
            var quantities = order.Items
                .Where(x => x.VariantId.HasValue)
                .GroupBy(x => x.VariantId!.Value)
                .ToDictionary(x => x.Key, x => checked((int)x.Sum(item => item.Quantity)));

            if (await _orders.TryMarkStockReleasedAsync(order.Id, now, cancellationToken))
                await _products.ReleaseVariantsStockAsync(quantities, cancellationToken);

            order.MarkStockReleased(now);
        }

        await _coupons.ReleaseByOrderAsync(order.Id, cancellationToken);
        await _flashSales.ReleaseQuotaByOrderAsync(order.Id, _clock.UtcNow, cancellationToken);
    }
}
