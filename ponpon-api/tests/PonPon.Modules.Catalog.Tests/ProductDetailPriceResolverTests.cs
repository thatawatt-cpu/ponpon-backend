using Microsoft.Extensions.Caching.Memory;
using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Application.Features.Products;
using PonPon.Modules.Catalog.Domain.FlashSales;
using PonPon.Modules.Catalog.Domain.Products;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Catalog.Tests;

public sealed class ProductDetailPriceResolverTests
{
    public void AppliesActiveFlashSaleDisplayPrice()
    {
        var nowUtc = new DateTime(2026, 7, 8, 5, 0, 0, DateTimeKind.Utc);
        var product = Product.CreateFromZort(new ProductSnapshot(
            ZortProductId: 1,
            ProductType: 0,
            Name: "Tea",
            Description: null,
            Sku: "TEA",
            Barcode: null,
            SellPrice: 100m,
            SellVatStatus: 0,
            PurchasePrice: null,
            PurchaseVatStatus: 0,
            Stock: 10,
            AvailableStock: 10,
            UnitText: null,
            ImageUrl: null,
            Weight: null,
            Height: null,
            Length: null,
            Width: null,
            ZortCategoryId: null,
            CategoryName: null,
            ZortSubCategoryId: null,
            SubCategoryName: null,
            ZortVariationId: null,
            IsActiveFromZort: true,
            RawZortJson: null),
            nowUtc);
        var flashSale = FlashSale.Create(
            "Noon sale",
            new DateOnly(2026, 7, 8),
            new DateOnly(2026, 7, 8),
            ["12:00-13:00"],
            [(product.Id, 80m, null)],
            nowUtc,
            true);
        var resolver = new ProductDetailPriceResolver(
            new FakeFlashSaleRepository(flashSale),
            new FakeClock(nowUtc),
            new MemoryCache(new MemoryCacheOptions()));

        var result = resolver.ResolveAsync(product).GetAwaiter().GetResult();

        AssertEqual(80m, result.DisplayPrice);
        AssertEqual(100m, result.DisplayOriginalPrice);
        AssertEqual(ProductDetailPriceSource.FlashSale, result.PriceSource);
        AssertEqual(flashSale.Id, result.ActiveFlashSaleId);
    }

    public void UsesBasePriceWhenFlashSaleIsInactive()
    {
        var nowUtc = new DateTime(2026, 7, 8, 5, 0, 0, DateTimeKind.Utc);
        var product = Product.CreateFromZort(new ProductSnapshot(
            1, 0, "Tea", null, "TEA", null, 100m, 0, null, 0, 10, 10,
            null, null, null, null, null, null, null, null, null, null, null,
            true, null),
            nowUtc);
        var flashSale = FlashSale.Create(
            "Noon sale",
            new DateOnly(2026, 7, 8),
            new DateOnly(2026, 7, 8),
            ["12:00-13:00"],
            [(product.Id, 80m, null)],
            nowUtc);
        var resolver = new ProductDetailPriceResolver(
            new FakeFlashSaleRepository(flashSale),
            new FakeClock(nowUtc),
            new MemoryCache(new MemoryCacheOptions()));

        var result = resolver.ResolveAsync(product).GetAwaiter().GetResult();

        AssertEqual(100m, result.DisplayPrice);
        AssertEqual(ProductDetailPriceSource.Base, result.PriceSource);
        AssertEqual(null, result.ActiveFlashSaleId);
    }

    public void ResolvesManyProductsWithOneActiveFlashSale()
    {
        var nowUtc = new DateTime(2026, 7, 8, 5, 0, 0, DateTimeKind.Utc);
        var first = Product.CreateFromZort(new ProductSnapshot(
            1, 0, "Tea", null, "TEA", null, 100m, 0, null, 0, 10, 10,
            null, null, null, null, null, null, null, null, null, null, null,
            true, null),
            nowUtc);
        var second = Product.CreateFromZort(new ProductSnapshot(
            2, 0, "Cake", null, "CAKE", null, 120m, 0, null, 0, 10, 10,
            null, null, null, null, null, null, null, null, null, null, null,
            true, null),
            nowUtc);
        var flashSale = FlashSale.Create(
            "Noon sale",
            new DateOnly(2026, 7, 8),
            new DateOnly(2026, 7, 8),
            ["12:00-13:00"],
            [(first.Id, 80m, null)],
            nowUtc,
            true);
        var resolver = new ProductDetailPriceResolver(
            new FakeFlashSaleRepository(flashSale),
            new FakeClock(nowUtc),
            new MemoryCache(new MemoryCacheOptions()));

        var result = resolver.ResolveAsync([first, second]).GetAwaiter().GetResult();

        AssertEqual(80m, result[first.Id].DisplayPrice);
        AssertEqual(ProductDetailPriceSource.FlashSale, result[first.Id].PriceSource);
        AssertEqual(120m, result[second.Id].DisplayPrice);
        AssertEqual(ProductDetailPriceSource.Base, result[second.Id].PriceSource);
    }


    private static void AssertEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected {expected}, but was {actual}.");
    }

    private sealed class FakeClock : IDateTimeProvider
    {
        public FakeClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }

    private sealed class FakeFlashSaleRepository : IFlashSaleRepository
    {
        private readonly IReadOnlyCollection<FlashSale> _flashSales;

        public FakeFlashSaleRepository(params FlashSale[] flashSales) => _flashSales = flashSales;
        public Task<IReadOnlyCollection<FlashSale>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_flashSales);
        public Task<FlashSale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_flashSales.FirstOrDefault(x => x.Id == id));
        public Task<FlashSale?> GetActiveAsync(DateOnly today, CancellationToken cancellationToken = default)
            => Task.FromResult(_flashSales.FirstOrDefault(x => x.StartDate <= today && today <= x.EndDate));
        public Task<IReadOnlyCollection<FlashSale>> GetActiveForProductsAsync(DateOnly today, IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyCollection<FlashSale>)_flashSales
                .Where(x => x.IsActive
                            && x.StartDate <= today
                            && today <= x.EndDate
                            && x.Products.Any(p => productIds.Contains(p.ProductId)))
                .ToArray());
        public Task AddAsync(FlashSale flashSale, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task DeleteProductsAsync(Guid flashSaleId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task<bool> TryReserveQuotaAsync(Guid orderId, Guid flashSaleId, IReadOnlyDictionary<Guid, int> productQuantities, DateTime nowUtc, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
        public Task ReleaseQuotaByOrderAsync(Guid orderId, DateTime nowUtc, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
