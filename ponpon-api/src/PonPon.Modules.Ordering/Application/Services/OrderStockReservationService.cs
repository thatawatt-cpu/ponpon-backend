using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Ordering.Domain.Orders;
using PonPon.Shared.Application.Abstractions;
using PonPon.Modules.Promotion.Application;

namespace PonPon.Modules.Ordering.Application.Services;

public sealed class OrderStockReservationService
{
    private readonly IProductRepository _products;
    private readonly IDateTimeProvider _clock;
    private readonly ICouponService _coupons;
    private readonly IFlashSaleRepository _flashSales;

    public OrderStockReservationService(
        IProductRepository products,
        ICouponService coupons,
        IFlashSaleRepository flashSales,
        IDateTimeProvider clock)
    {
        _products = products;
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
            var quantities = order.Items
                .Where(x => x.VariantId.HasValue)
                .GroupBy(x => x.VariantId!.Value)
                .ToDictionary(x => x.Key, x => checked((int)x.Sum(item => item.Quantity)));

            await _products.TryReleaseOrderVariantsStockAsync(
                order.Id,
                quantities,
                _clock.UtcNow,
                cancellationToken);
            order.MarkStockReleased(_clock.UtcNow);
        }

        await _coupons.ReleaseByOrderAsync(order.Id, cancellationToken);
        await _flashSales.ReleaseQuotaByOrderAsync(order.Id, _clock.UtcNow, cancellationToken);
    }
}
