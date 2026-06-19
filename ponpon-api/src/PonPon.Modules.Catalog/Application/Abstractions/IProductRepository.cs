using PonPon.Modules.Catalog.Domain.Categories;
using PonPon.Modules.Catalog.Domain.Products;

namespace PonPon.Modules.Catalog.Application.Abstractions;

public interface IProductRepository
{
    Task<IReadOnlyCollection<Product>> GetCustomerProductsAsync(string? keyword, string? category, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Product>> GetAdminProductsAsync(string? keyword, ProductStatus? status, ProductSource? source, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Product>> GetByIdsAsync(IReadOnlySet<Guid> ids, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdWithVariantsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdWithVariantsAndImagesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product?> GetBySlugWithVariantsAndImagesAsync(string slug, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdWithImagesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product?> GetByZortProductIdAsync(long zortProductId, CancellationToken cancellationToken = default);
    Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Product>> GetByZortProductIdsOrSkusAsync(IReadOnlySet<long> zortProductIds, IReadOnlySet<string> skus, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Product>> GetByBaseSkusWithVariantsAsync(IReadOnlySet<string> baseSkus, CancellationToken cancellationToken = default);
    Task<ProductVariant?> GetVariantByIdAsync(Guid variantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Product>> GetLiffZortProductsNotSeenAsync(IReadOnlySet<long> seenZortProductIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Category>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Category>> GetCategoriesAsync(IReadOnlySet<long> zortCategoryIds, IReadOnlySet<string> names, CancellationToken cancellationToken = default);
    Task<Category?> GetCategoryAsync(long? zortCategoryId, string name, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteProductImagesAsync(Guid productId, CancellationToken cancellationToken = default);
    Task AddProductImagesAsync(IReadOnlyList<ProductImage> images, CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Product> products, CancellationToken cancellationToken = default);
    Task AddCategoryAsync(Category category, CancellationToken cancellationToken = default);
    Task AddCategoriesAsync(IEnumerable<Category> categories, CancellationToken cancellationToken = default);
}
