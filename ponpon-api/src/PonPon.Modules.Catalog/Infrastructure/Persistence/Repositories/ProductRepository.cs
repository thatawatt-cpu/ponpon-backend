using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Linq.Expressions;
using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Application.Features.FlashSales.GetFlashSales;
using PonPon.Modules.Catalog.Domain.Categories;
using PonPon.Modules.Catalog.Domain.Products;
using PonPon.Modules.Catalog.Infrastructure.ExternalServices.Zort;
using PonPon.Shared.Application.Abstractions;

namespace PonPon.Modules.Catalog.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly CatalogDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly IDateTimeProvider _clock;

    public ProductRepository(CatalogDbContext dbContext, IMemoryCache cache, IDateTimeProvider clock)
    {
        _dbContext = dbContext;
        _cache = cache;
        _clock = clock;
    }

    public async Task<IReadOnlyCollection<Product>> GetCustomerProductsAsync(string? keyword, string? category, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products
            .AsNoTracking()
            .Include(x => x.Variants)
            .Where(x => x.IsActiveFromZort && x.IsVisibleOnLiff && x.Status == ProductStatus.Active && x.AvailableStock > 0);
        query = ApplyKeyword(query, keyword);
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(x => x.CategoryName == category);
        }

        return await query.OrderBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Application.Features.Products.GetProducts.ProductListItemReadModel>> GetCustomerProductListItemsAsync(string? keyword, string? category, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products
            .AsNoTracking()
            .Where(x => x.IsActiveFromZort && x.IsVisibleOnLiff && x.Status == ProductStatus.Active && x.AvailableStock > 0);
        query = ApplyKeyword(query, keyword);
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(x => x.CategoryName == category);
        }

        var products = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ProductListPageRow(
                x.Id,
                x.Name,
                x.BaseSku,
                x.Slug,
                x.SellPrice,
                x.OriginalPrice,
                x.Stock,
                x.AvailableStock,
                x.ImageUrl,
                x.CategoryName,
                x.IsActiveFromZort,
                x.IsVisibleOnLiff,
                x.Source,
                x.Status))
            .ToArrayAsync(cancellationToken);

        return await HydrateListRowsAsync(products, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Product>> GetFeaturedCustomerProductsAsync(int limit, CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(limit, 1, 100);
        return await _dbContext.Products
            .AsNoTracking()
            .Include(x => x.Variants)
            .Where(x => x.IsActiveFromZort
                        && x.IsVisibleOnLiff
                        && x.Status == ProductStatus.Active
                        && x.AvailableStock > 0)
            .OrderByDescending(x => x.IsFeatured)
            .ThenByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .ThenBy(x => x.Name)
            .Take(take)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Product>> GetRelatedCustomerProductsAsync(Guid productId, string? categoryName, int limit, CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(limit, 1, 100);
        var profile = await GetRelatedProductProfileAsync(productId, categoryName, cancellationToken);
        var activeFlashSaleProductIds = await GetActiveFlashSaleRelatedProductIdsAsync(productId, cancellationToken);

        return await BuildRelatedProductQuery(productId)
            .Include(x => x.Variants)
            .OrderByDescending(RelatedProductScoreExpression(profile, activeFlashSaleProductIds))
            .ThenByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .ThenBy(x => x.Name)
            .Take(take)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Application.Features.Products.GetProducts.ProductListItemReadModel>> GetRelatedCustomerProductListItemsAsync(Guid productId, string? categoryName, int limit, CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(limit, 1, 100);
        var localNow = GetFlashSalesHandler.GetBangkokNow(_clock.UtcNow);
        var cacheKey = $"catalog:related-list:{productId:N}:{NormalizeCachePart(categoryName)}:{localNow:yyyyMMddHHmm}:{take}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyCollection<Application.Features.Products.GetProducts.ProductListItemReadModel>? cached)
            && cached is not null)
        {
            return cached;
        }

        var profile = await GetRelatedProductProfileAsync(productId, categoryName, cancellationToken);
        var activeFlashSaleProductIds = await GetActiveFlashSaleRelatedProductIdsAsync(productId, cancellationToken);

        var products = await BuildRelatedProductQuery(productId)
            .OrderByDescending(RelatedProductScoreExpression(profile, activeFlashSaleProductIds))
            .ThenByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .ThenBy(x => x.Name)
            .Take(take)
            .Select(x => new ProductListPageRow(
                x.Id,
                x.Name,
                x.BaseSku,
                x.Slug,
                x.SellPrice,
                x.OriginalPrice,
                x.Stock,
                x.AvailableStock,
                x.ImageUrl,
                x.CategoryName,
                x.IsActiveFromZort,
                x.IsVisibleOnLiff,
                x.Source,
                x.Status))
            .ToArrayAsync(cancellationToken);

        var result = await HydrateListRowsAsync(products, cancellationToken);
        _cache.Set(cacheKey, result, RelatedListCacheOptions);
        return result;
    }

    private IQueryable<Product> BuildRelatedProductQuery(Guid productId)
    {
        return _dbContext.Products
            .AsNoTracking()
            .Where(x => x.Id != productId
                        && x.IsActiveFromZort
                        && x.IsVisibleOnLiff
                        && x.Status == ProductStatus.Active
                        && x.AvailableStock > 0);
    }

    private static Expression<Func<Product, int>> RelatedProductScoreExpression(
        RelatedProductProfile profile,
        IReadOnlySet<Guid> activeFlashSaleProductIds)
    {
        var hasSubCategory = !string.IsNullOrWhiteSpace(profile.SubCategoryName);
        var hasCategory = !string.IsNullOrWhiteSpace(profile.CategoryName);
        var flashSaleProductIds = activeFlashSaleProductIds.ToArray();

        return product =>
            (hasSubCategory && product.SubCategoryName == profile.SubCategoryName ? 60 : 0)
            + (hasCategory && product.CategoryName == profile.CategoryName ? 40 : 0)
            + (flashSaleProductIds.Contains(product.Id) ? 15 : 0)
            + (product.IsBestSeller ? 15 : 0)
            + (product.IsFeatured ? 10 : 0);
    }

    private async Task<RelatedProductProfile> GetRelatedProductProfileAsync(
        Guid productId,
        string? categoryName,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .Where(x => x.Id == productId)
            .Select(x => new RelatedProductProfile(
                string.IsNullOrWhiteSpace(categoryName) ? x.CategoryName : categoryName,
                x.SubCategoryName))
            .FirstOrDefaultAsync(cancellationToken);

        return product ?? new RelatedProductProfile(categoryName, null);
    }

    private async Task<IReadOnlySet<Guid>> GetActiveFlashSaleRelatedProductIdsAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var localNow = GetFlashSalesHandler.GetBangkokNow(_clock.UtcNow);
        var today = DateOnly.FromDateTime(localNow);

        var flashSales = await _dbContext.FlashSales
            .AsNoTracking()
            .Include(x => x.Products)
            .Where(x => x.IsActive
                        && x.StartDate <= today
                        && today <= x.EndDate
                        && x.Products.Any(p => p.ProductId == productId))
            .ToArrayAsync(cancellationToken);

        return flashSales
            .Where(x => GetFlashSalesHandler.IsActiveNow(x, localNow))
            .SelectMany(x => x.Products.Select(p => p.ProductId))
            .ToHashSet();
    }

    public async Task<IReadOnlyCollection<Product>> GetAdminProductsAsync(string? keyword, ProductStatus? status, ProductSource? source, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products.AsNoTracking().Include(x => x.Variants).AsQueryable();
        query = ApplyKeyword(query, keyword);
        if (status is not null)
        {
            query = query.Where(x => x.Status == status);
        }

        if (source is not null)
        {
            query = query.Where(x => x.Source == source);
        }

        return await query.OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Application.Features.Products.GetProducts.ProductListItemReadModel>> GetAdminProductListItemsAsync(string? keyword, ProductStatus? status, ProductSource? source, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products.AsNoTracking().AsQueryable();
        query = ApplyKeyword(query, keyword);
        if (status is not null)
        {
            query = query.Where(x => x.Status == status);
        }

        if (source is not null)
        {
            query = query.Where(x => x.Source == source);
        }

        var products = await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ProductListPageRow(
                x.Id,
                x.Name,
                x.BaseSku,
                x.Slug,
                x.SellPrice,
                x.OriginalPrice,
                x.Stock,
                x.AvailableStock,
                x.ImageUrl,
                x.CategoryName,
                x.IsActiveFromZort,
                x.IsVisibleOnLiff,
                x.Source,
                x.Status))
            .ToArrayAsync(cancellationToken);

        return await HydrateListRowsAsync(products, cancellationToken);
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => _dbContext.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Product>> GetByIdsAsync(IReadOnlySet<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0) return [];
        return await _dbContext.Products.AsNoTracking().Where(x => ids.Contains(x.Id)).ToArrayAsync(cancellationToken);
    }

    public Task<Product?> GetByIdWithVariantsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products
            .AsNoTracking()
            .Include(x => x.Variants)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Product>> GetByIdsWithVariantsAsync(IReadOnlySet<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return [];

        return await _dbContext.Products
            .AsNoTracking()
            .Include(x => x.Variants)
            .Where(x => ids.Contains(x.Id))
            .ToArrayAsync(cancellationToken);
    }

    public Task<Product?> GetByIdWithVariantsAndImagesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Variants)
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Product?> GetBySlugWithVariantsAndImagesAsync(string slug, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Variants)
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);
    }

    public Task<Product?> GetByIdWithImagesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Product?> GetByZortProductIdAsync(long zortProductId, CancellationToken cancellationToken = default) => _dbContext.Products.FirstOrDefaultAsync(x => x.ZortProductId == zortProductId, cancellationToken);

    public Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default) => _dbContext.Products.FirstOrDefaultAsync(x => x.BaseSku == sku, cancellationToken);

    public async Task<IReadOnlyCollection<Product>> GetByZortProductIdsOrSkusAsync(IReadOnlySet<long> zortProductIds, IReadOnlySet<string> skus, CancellationToken cancellationToken = default)
    {
        if (zortProductIds.Count == 0 && skus.Count == 0)
        {
            return [];
        }

        return await _dbContext.Products
            .Include(x => x.Variants)
            .Where(x => (x.ZortProductId.HasValue && zortProductIds.Contains(x.ZortProductId.Value)) || (x.BaseSku != null && skus.Contains(x.BaseSku)) || x.Variants.Any(v => skus.Contains(v.Sku)))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Product>> GetByBaseSkusWithVariantsAsync(IReadOnlySet<string> baseSkus, CancellationToken cancellationToken = default)
    {
        if (baseSkus.Count == 0)
        {
            return [];
        }

        return await _dbContext.Products
            .Include(x => x.Variants)
            .Where(x => x.BaseSku != null && baseSkus.Contains(x.BaseSku))
            .ToArrayAsync(cancellationToken);
    }

    public Task<ProductVariant?> GetVariantByIdAsync(Guid variantId, CancellationToken cancellationToken = default)
    {
        return _dbContext.ProductVariants.FirstOrDefaultAsync(x => x.Id == variantId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Product>> GetLiffZortProductsNotSeenAsync(IReadOnlySet<long> seenZortProductIds, CancellationToken cancellationToken = default)
    {
        var candidates = await _dbContext.Products
            .Where(x => x.Source == ProductSource.Zort && x.ZortProductId.HasValue && !seenZortProductIds.Contains(x.ZortProductId.Value))
            .ToArrayAsync(cancellationToken);

        return candidates
            .Where(x => ZortProductTagMatcher.HasLiffTag(x.RawZortJson))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<Category>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Category>> GetCategoriesAsync(IReadOnlySet<long> zortCategoryIds, IReadOnlySet<string> names, CancellationToken cancellationToken = default)
    {
        if (zortCategoryIds.Count == 0 && names.Count == 0)
        {
            return [];
        }

        return await _dbContext.Categories
            .Where(x => (x.ZortCategoryId.HasValue && zortCategoryIds.Contains(x.ZortCategoryId.Value)) || names.Contains(x.Name))
            .ToArrayAsync(cancellationToken);
    }

    public Task<Category?> GetCategoryAsync(long? zortCategoryId, string name, CancellationToken cancellationToken = default)
    {
        return zortCategoryId.HasValue
            ? _dbContext.Categories.FirstOrDefaultAsync(x => x.ZortCategoryId == zortCategoryId.Value, cancellationToken)
            : _dbContext.Categories.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Products.AnyAsync(x => x.Id == id, cancellationToken);

    public async Task DeleteProductImagesAsync(Guid productId, CancellationToken cancellationToken = default)
        => await _dbContext.ProductImages.Where(x => x.ProductId == productId).ExecuteDeleteAsync(cancellationToken);

    public async Task AddProductImagesAsync(IReadOnlyList<ProductImage> images, CancellationToken cancellationToken = default)
        => await _dbContext.ProductImages.AddRangeAsync(images, cancellationToken);

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default) => await _dbContext.Products.AddAsync(product, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<Product> products, CancellationToken cancellationToken = default) => await _dbContext.Products.AddRangeAsync(products, cancellationToken);

    public async Task AddCategoryAsync(Category category, CancellationToken cancellationToken = default) => await _dbContext.Categories.AddAsync(category, cancellationToken);

    public async Task AddCategoriesAsync(IEnumerable<Category> categories, CancellationToken cancellationToken = default) => await _dbContext.Categories.AddRangeAsync(categories, cancellationToken);

    private async Task<IReadOnlyCollection<Application.Features.Products.GetProducts.ProductListItemReadModel>> HydrateListRowsAsync(
        IReadOnlyCollection<ProductListPageRow> products,
        CancellationToken cancellationToken)
    {
        if (products.Count == 0)
            return [];

        var productIds = products.Select(x => x.Id).ToArray();
        var variantRows = await _dbContext.ProductVariants
            .AsNoTracking()
            .Where(x => productIds.Contains(x.ProductId))
            .Select(x => new ProductListVariantRow(x.ProductId, x.Stock, x.AvailableStock, x.ImageUrl))
            .ToArrayAsync(cancellationToken);

        var variantsByProduct = variantRows
            .GroupBy(x => x.ProductId)
            .ToDictionary(x => x.Key, x => x.ToArray());

        return products.Select(product =>
        {
            variantsByProduct.TryGetValue(product.Id, out var variants);
            variants ??= [];

            return new Application.Features.Products.GetProducts.ProductListItemReadModel(
                product.Id,
                product.Name,
                product.BaseSku,
                product.Slug,
                product.SellPrice,
                product.OriginalPrice,
                product.Stock,
                product.AvailableStock,
                product.ImageUrl,
                product.CategoryName,
                product.IsActiveFromZort,
                product.IsVisibleOnLiff,
                product.Source,
                product.Status,
                variants.Sum(x => x.Stock),
                variants.Sum(x => x.AvailableStock),
                variants.Length,
                variants.Select(x => x.ImageUrl).OfType<string>().ToArray());
        }).ToArray();
    }

    private static IQueryable<Product> ApplyKeyword(IQueryable<Product> query, string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return query;
        }

        var normalized = keyword.Trim();
        return query.Where(x => x.Name.Contains(normalized) || (x.BaseSku != null && x.BaseSku.Contains(normalized)) || (x.Barcode != null && x.Barcode.Contains(normalized)));
    }

    private sealed record ProductListPageRow(
        Guid Id,
        string Name,
        string? BaseSku,
        string? Slug,
        decimal SellPrice,
        decimal? OriginalPrice,
        int Stock,
        int AvailableStock,
        string? ImageUrl,
        string? CategoryName,
        bool IsActiveFromZort,
        bool IsVisibleOnLiff,
        ProductSource Source,
        ProductStatus Status);

    private sealed record ProductListVariantRow(Guid ProductId, int Stock, int AvailableStock, string? ImageUrl);

    private sealed record RelatedProductProfile(string? CategoryName, string? SubCategoryName);

    private static readonly MemoryCacheEntryOptions RelatedListCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30),
        SlidingExpiration = TimeSpan.FromSeconds(10),
        Size = 1
    };

    private static string NormalizeCachePart(string? value)
        => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim().ToLowerInvariant();

    public async Task<IReadOnlyDictionary<string, (string? ImageUrl, string? OptionsJson)>> GetVariantImageAndOptionsBySkusAsync(
        IReadOnlySet<string> skus,
        CancellationToken cancellationToken = default)
    {
        var results = await _dbContext.Set<ProductVariant>()
            .AsNoTracking()
            .Where(x => skus.Contains(x.Sku))
            .Select(x => new { x.Sku, x.ImageUrl, x.OptionsJson })
            .ToArrayAsync(cancellationToken);

        return results.ToDictionary(x => x.Sku, x => (x.ImageUrl, x.OptionsJson));
    }

    public async Task<bool> TryReserveVariantsStockAsync(
        IReadOnlyDictionary<Guid, int> variantQuantities,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        foreach (var (variantId, quantity) in variantQuantities)
        {
            if (quantity <= 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var affected = await _dbContext.Set<ProductVariant>()
                .Where(x => x.Id == variantId && x.AvailableStock >= quantity)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(
                        v => v.AvailableStock,
                        v => v.AvailableStock - quantity),
                    cancellationToken);

            if (affected != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task ReleaseVariantsStockAsync(
        IReadOnlyDictionary<Guid, int> variantQuantities,
        CancellationToken cancellationToken = default)
    {
        foreach (var (variantId, quantity) in variantQuantities.Where(x => x.Value > 0))
        {
            await _dbContext.Set<ProductVariant>()
                .Where(x => x.Id == variantId)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(
                        v => v.AvailableStock,
                        v => v.AvailableStock + quantity),
                    cancellationToken);
        }
    }

}
