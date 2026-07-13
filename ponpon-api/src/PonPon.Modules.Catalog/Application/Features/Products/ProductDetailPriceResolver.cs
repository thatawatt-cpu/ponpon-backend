using Microsoft.Extensions.Caching.Memory;
using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Application.Features.FlashSales.GetFlashSales;
using PonPon.Modules.Catalog.Domain.Products;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Catalog.Application.Features.Products;

public sealed class ProductDetailPriceResolver
{
    private readonly IFlashSaleRepository _flashSales;
    private readonly IDateTimeProvider _clock;
    private readonly IMemoryCache _cache;

    public ProductDetailPriceResolver(IFlashSaleRepository flashSales, IDateTimeProvider clock, IMemoryCache cache)
    {
        _flashSales = flashSales;
        _clock = clock;
        _cache = cache;
    }

    public async Task<ProductDetailPrice> ResolveAsync(Product product, CancellationToken cancellationToken = default)
    {
        var prices = await ResolveAsync([product], cancellationToken);
        return prices[product.Id];
    }

    public async Task<IReadOnlyDictionary<Guid, ProductDetailPrice>> ResolveAsync(
        IReadOnlyCollection<Product> products,
        CancellationToken cancellationToken = default)
    {
        return await ResolveAsync(
            products.Select(x => new ProductPriceInput(x.Id, x.SellPrice, x.OriginalPrice)).ToArray(),
            cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, ProductDetailPrice>> ResolveAsync(
        IReadOnlyCollection<GetProducts.ProductListItemReadModel> products,
        CancellationToken cancellationToken = default)
    {
        return await ResolveAsync(
            products.Select(x => new ProductPriceInput(x.Id, x.SellPrice, x.OriginalPrice)).ToArray(),
            cancellationToken);
    }

    private async Task<IReadOnlyDictionary<Guid, ProductDetailPrice>> ResolveAsync(
        IReadOnlyCollection<ProductPriceInput> products,
        CancellationToken cancellationToken = default)
    {
        if (products.Count == 0)
            return new Dictionary<Guid, ProductDetailPrice>();

        var productMap = products.ToDictionary(x => x.Id);
        var localNow = GetFlashSalesHandler.GetBangkokNow(_clock.UtcNow);
        var today = DateOnly.FromDateTime(localNow);
        var productIds = productMap.Keys.OrderBy(x => x).ToArray();
        var cacheKey = $"catalog:active-flash-prices:{today:yyyyMMdd}:{string.Join(',', productIds.Select(x => x.ToString("N")))}";
        var flashSales = await _cache.GetOrCreateAsync(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20);
            entry.SlidingExpiration = TimeSpan.FromSeconds(10);
            entry.Size = 1;
            return _flashSales.GetActiveForProductsAsync(today, productIds, cancellationToken);
        }) ?? [];

        var flashSale = flashSales
            .Where(x => GetFlashSalesHandler.IsActiveNow(x, localNow))
            .Where(x => x.Products.Any(p =>
                productMap.TryGetValue(p.ProductId, out var product) && p.SalePrice < product.SellPrice))
            .OrderByDescending(x => x.StartDate)
            .FirstOrDefault();

        var flashSaleProductMap = flashSale?.Products
            .Where(x => productMap.ContainsKey(x.ProductId))
            .ToDictionary(x => x.ProductId);

        return products.ToDictionary(x => x.Id, product =>
        {
            if (flashSale is not null
                && flashSaleProductMap is not null
                && flashSaleProductMap.TryGetValue(product.Id, out var flashSaleProduct)
                && flashSaleProduct.SalePrice < product.SellPrice)
            {
                return new ProductDetailPrice(
                    flashSaleProduct.SalePrice,
                    product.SellPrice,
                    ProductDetailPriceSource.FlashSale,
                    flashSale.Id);
            }

            return new ProductDetailPrice(
                product.SellPrice,
                product.OriginalPrice,
                ProductDetailPriceSource.Base,
                null);
        });
    }
}

internal sealed record ProductPriceInput(Guid Id, decimal SellPrice, decimal? OriginalPrice);

public sealed record ProductDetailPrice(
    decimal DisplayPrice,
    decimal? DisplayOriginalPrice,
    string PriceSource,
    Guid? ActiveFlashSaleId);

public static class ProductDetailPriceSource
{
    public const string Base = "base";
    public const string FlashSale = "flash_sale";
}
