using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Catalog.Application.Abstractions;
using PonPon.Modules.Catalog.Domain.Categories;
using PonPon.Modules.Catalog.Domain.Products;
using PonPon.Modules.Catalog.Infrastructure.ExternalServices.Zort;

namespace PonPon.Modules.Catalog.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly CatalogDbContext _dbContext;

    public ProductRepository(CatalogDbContext dbContext) => _dbContext = dbContext;

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

    public Task<Product?> GetByIdWithVariantsAndImagesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products
            .AsNoTracking()
            .Include(x => x.Variants)
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Product?> GetBySlugWithVariantsAndImagesAsync(string slug, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products
            .AsNoTracking()
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

    private static IQueryable<Product> ApplyKeyword(IQueryable<Product> query, string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return query;
        }

        var normalized = keyword.Trim();
        return query.Where(x => x.Name.Contains(normalized) || (x.BaseSku != null && x.BaseSku.Contains(normalized)) || (x.Barcode != null && x.Barcode.Contains(normalized)));
    }

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

    public async Task<bool> TryReleaseOrderVariantsStockAsync(
        Guid orderId,
        IReadOnlyDictionary<Guid, int> variantQuantities,
        DateTime releasedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var claimed = await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             UPDATE ordering.orders
             SET "HasStockReservation" = FALSE,
                 "UpdatedAtUtc" = {releasedAtUtc}
             WHERE "Id" = {orderId}
               AND "HasStockReservation" = TRUE
             """,
            cancellationToken);

        if (claimed != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        foreach (var (variantId, quantity) in variantQuantities.Where(x => x.Value > 0))
        {
            var affected = await _dbContext.Set<ProductVariant>()
                .Where(x => x.Id == variantId)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(
                        v => v.AvailableStock,
                        v => v.AvailableStock + quantity),
                    cancellationToken);

            if (affected != 1)
                throw new InvalidOperationException($"Variant {variantId} was not found while releasing stock.");
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
