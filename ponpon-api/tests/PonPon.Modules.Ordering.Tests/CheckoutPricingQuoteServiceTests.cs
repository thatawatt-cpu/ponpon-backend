using Microsoft.Extensions.Options;
using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Domain.Categories;
using PonPon.Modules.Catalog.Domain.Products;
using PonPon.Modules.Ordering.Application.Features.Orders.CheckoutPricing;
using PonPon.Modules.Ordering.Application.Pricing;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Ordering.Tests;

public sealed class CheckoutPricingQuoteServiceTests
{
    public void CreatesPartialQuoteWithoutShippingDetails()
    {
        var product = CreateProduct();
        var shippingRates = new FakeShippingRateQuoteService(45m);
        var service = CreateService(product, shippingRates);

        var draft = service.CalculateAsync(new CheckoutPricingPayload(
            null,
            null,
            null,
            null,
            "standard",
            null,
            [new CheckoutPricingItem(product.Id, product.Variants.Single().Id, 2)]))
            .GetAwaiter()
            .GetResult();

        AssertEqual(false, draft.IsFinal);
        AssertEqual(false, draft.ShippingFinalized);
        AssertEqual("partial", draft.CalculationStatus);
        AssertEqual(0m, draft.Pricing.ShippingAmount);
        AssertEqual(200m, draft.Pricing.GrandTotal);
        AssertEqual(0, shippingRates.CallCount);
    }

    public void FinalizesQuoteWhenShippingDetailsAreComplete()
    {
        var product = CreateProduct();
        var shippingRates = new FakeShippingRateQuoteService(45m);
        var service = CreateService(product, shippingRates);

        var draft = service.CalculateAsync(new CheckoutPricingPayload(
            "buyer@example.com",
            "Buyer",
            "0812345678",
            "99 Road district state province 10110",
            "standard",
            null,
            [new CheckoutPricingItem(product.Id, product.Variants.Single().Id, 2)]))
            .GetAwaiter()
            .GetResult();

        AssertEqual(true, draft.IsFinal);
        AssertEqual(true, draft.ShippingFinalized);
        AssertEqual("final", draft.CalculationStatus);
        AssertEqual(45m, draft.Pricing.ShippingAmount);
        AssertEqual(245m, draft.Pricing.GrandTotal);
        AssertEqual(1, shippingRates.CallCount);
    }

    public void SelectsCheapestShippingChannelWhenNotProvided()
    {
        var product = CreateProduct();
        var shippingRates = new FakeShippingRateQuoteService(
            45m,
            [
                new ShippingRateQuoteOption("EXPRESS", 80m),
                new ShippingRateQuoteOption("ECONOMY", 45m)
            ]);
        var service = CreateService(product, shippingRates);

        var draft = service.CalculateAsync(new CheckoutPricingPayload(
            "buyer@example.com",
            "Buyer",
            "0812345678",
            "99 Road district state province 10110",
            null,
            null,
            [new CheckoutPricingItem(product.Id, product.Variants.Single().Id, 1)]))
            .GetAwaiter()
            .GetResult();

        AssertEqual(true, draft.IsFinal);
        AssertEqual("ECONOMY", draft.ShippingChannel);
        AssertEqual(145m, draft.Pricing.GrandTotal);
    }

    private static CheckoutPricingQuoteService CreateService(
        Product product,
        IShippingRateQuoteService shippingRates)
        => new(
            new FakeProductRepository(product),
            shippingRates,
            new PricingPipeline([new FinalizePricingStep(Options.Create(new PricingOptions()))]),
            new FakeDateTimeProvider(),
            new FakeCurrentUser());

    private static Product CreateProduct()
    {
        var now = new DateTime(2026, 7, 17, 3, 0, 0, DateTimeKind.Utc);
        var snapshot = new ProductSnapshot(
            1,
            0,
            "Tea",
            null,
            "TEA-RED",
            null,
            100m,
            0,
            null,
            0,
            10,
            10,
            "pack",
            null,
            200m,
            6m,
            10m,
            8m,
            null,
            null,
            null,
            null,
            null,
            true,
            "{}");
        var product = Product.CreateFromZort(snapshot, now);
        product.SetVisibility(true, now);
        product.UpsertVariant(snapshot, now);
        return product;
    }

    private static void AssertEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected {expected}, but was {actual}.");
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = new(2026, 7, 17, 3, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? CustomerId { get; } = Guid.NewGuid();
        public string? UserType => "Customer";
        public string? LineUserId => "line-user";
        public IReadOnlyCollection<string> Roles => [];
    }

    private sealed class FakeShippingRateQuoteService(
        decimal amount,
        IReadOnlyCollection<ShippingRateQuoteOption>? options = null) : IShippingRateQuoteService
    {
        public int CallCount { get; private set; }

        public Task<decimal> GetShippingAmountAsync(
            ShippingRateQuoteRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            var option = options?.FirstOrDefault(x =>
                string.Equals(x.ShippingChannel, request.ShippingChannel, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(option?.Amount ?? amount);
        }

        public Task<IReadOnlyCollection<ShippingRateQuoteOption>> GetShippingOptionsAsync(
            ShippingRateQuoteRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(options ?? [new ShippingRateQuoteOption(request.ShippingChannel, amount)]);
    }

    private sealed class FakeProductRepository(Product product) : IProductRepository
    {
        public Task<IReadOnlyCollection<Product>> GetByIdsWithVariantsAsync(
            IReadOnlySet<Guid> ids,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<Product>>(ids.Contains(product.Id) ? [product] : []);

        public Task<Product?> GetByIdWithVariantsAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<Product?>(id == product.Id ? product : null);

        public Task<IReadOnlyCollection<Product>> GetCustomerProductsAsync(string? keyword, string? category, int page, int pageSize, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<IReadOnlyCollection<PonPon.Modules.Catalog.Application.Features.Products.GetProducts.ProductListItemReadModel>> GetCustomerProductListItemsAsync(string? keyword, string? category, int page, int pageSize, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Product>> GetFeaturedCustomerProductsAsync(int limit, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Product>> GetRelatedCustomerProductsAsync(Guid productId, string? categoryName, int limit, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<IReadOnlyCollection<PonPon.Modules.Catalog.Application.Features.Products.GetProducts.ProductListItemReadModel>> GetRelatedCustomerProductListItemsAsync(Guid productId, string? categoryName, int limit, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Product>> GetAdminProductsAsync(string? keyword, ProductStatus? status, ProductSource? source, int page, int pageSize, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<IReadOnlyCollection<PonPon.Modules.Catalog.Application.Features.Products.GetProducts.ProductListItemReadModel>> GetAdminProductListItemsAsync(string? keyword, ProductStatus? status, ProductSource? source, int page, int pageSize, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<PonPon.Modules.Catalog.Application.Features.Products.GetProducts.ProductListPageReadModel> GetAdminProductListPageAsync(string? keyword, string? category, ProductStatus? status, ProductSource? source, int page, int pageSize, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Product>> GetByIdsAsync(IReadOnlySet<Guid> ids, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<Product?> GetByIdWithVariantsAndImagesAsync(Guid id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<PonPon.Modules.Catalog.Application.Features.Products.GetProductById.ProductDetailReadModel?> GetDetailBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<Product?> GetBySlugWithVariantsAndImagesAsync(string slug, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<Product?> GetByIdWithImagesAsync(Guid id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<Product?> GetByZortProductIdAsync(long zortProductId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Product>> GetByZortProductIdsOrSkusAsync(IReadOnlySet<long> zortProductIds, IReadOnlySet<string> skus, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Product>> GetByBaseSkusWithVariantsAsync(IReadOnlySet<string> baseSkus, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<ProductVariant?> GetVariantByIdAsync(Guid variantId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Product>> GetLiffZortProductsNotSeenAsync(IReadOnlySet<long> seenZortProductIds, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Category>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<IReadOnlyCollection<Category>> GetCategoriesAsync(IReadOnlySet<long> zortCategoryIds, IReadOnlySet<string> names, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<Category?> GetCategoryAsync(long? zortCategoryId, string name, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task DeleteProductImagesAsync(Guid productId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task AddProductImagesAsync(IReadOnlyList<ProductImage> images, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task AddAsync(Product product, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task AddRangeAsync(IEnumerable<Product> products, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task AddCategoryAsync(Category category, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task AddCategoriesAsync(IEnumerable<Category> categories, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<bool> TryReserveVariantsStockAsync(IReadOnlyDictionary<Guid, int> variantQuantities, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task ReleaseVariantsStockAsync(IReadOnlyDictionary<Guid, int> variantQuantities, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<string, (string? ImageUrl, string? OptionsJson)>> GetVariantImageAndOptionsBySkusAsync(IReadOnlySet<string> skus, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
